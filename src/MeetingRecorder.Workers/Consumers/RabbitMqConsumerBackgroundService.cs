using System.Text;
using System.Text.Json;
using MeetingRecorder.Infrastructure.Options;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MeetingRecorder.Workers.Consumers;

/// <summary>
/// Base for a topic-exchange consumer bound to a single routing key. Acknowledges only
/// after HandleAsync completes successfully so a crash mid-processing safely redelivers.
/// Failures are retried with exponential backoff (tracked via an "x-retry-count" header on
/// a republished copy of the message) up to MaxRetryAttempts, then routed to "{routingKey}.dlq".
/// </summary>
public abstract class RabbitMqConsumerBackgroundService<TMessage>(
    IConnection connection,
    IOptions<RabbitMqOptions> options,
    ILogger logger) : BackgroundService
    where TMessage : class
{
    private readonly RabbitMqOptions _options = options.Value;
    protected abstract string RoutingKey { get; }

    /// <summary>
    /// Queue name for this consumer. Defaults to the routing key, but must be overridden
    /// with a unique value when more than one consumer type binds to the same routing key
    /// (e.g. transcription and diarization both consume "recording.uploaded") — otherwise
    /// they'd share one queue and compete for messages instead of both processing every one.
    /// </summary>
    protected virtual string QueueName => RoutingKey;

    protected abstract Task HandleAsync(TMessage message, CancellationToken ct);

    /// <summary>Called once retries are exhausted and the message has been dead-lettered,
    /// so the meeting can be surfaced as Failed. Default no-op.</summary>
    protected virtual Task OnExhaustedAsync(byte[] body, CancellationToken ct) => Task.CompletedTask;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await channel.ExchangeDeclareAsync(_options.ExchangeName, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);

        var queueName = QueueName;
        await channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await channel.QueueBindAsync(queueName, _options.ExchangeName, RoutingKey, cancellationToken: stoppingToken);

        var dlqName = $"{queueName}.dlq";
        await channel.QueueDeclareAsync(dlqName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await channel.QueueBindAsync(dlqName, _options.ExchangeName, dlqName, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) => await OnMessageAsync(channel, ea, stoppingToken);

        await channel.BasicConsumeAsync(queueName, autoAck: false, consumer, cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken).ContinueWith(_ => { }, TaskScheduler.Default);
    }

    private async Task OnMessageAsync(IChannel channel, BasicDeliverEventArgs ea, CancellationToken ct)
    {
        var retryCount = GetRetryCount(ea.BasicProperties);

        try
        {
            var message = JsonSerializer.Deserialize<TMessage>(ea.Body.Span)
                ?? throw new InvalidOperationException($"Could not deserialize {typeof(TMessage).Name}.");

            await HandleAsync(message, ct);
            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process {Message} (attempt {Attempt})", typeof(TMessage).Name, retryCount + 1);
            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, ct);

            if (retryCount + 1 >= _options.MaxRetryAttempts)
            {
                await PublishAsync(channel, $"{QueueName}.dlq", ea.Body.ToArray(), retryCount + 1, ct);
                await OnExhaustedAsync(ea.Body.ToArray(), ct);
                return;
            }

            var backoff = TimeSpan.FromSeconds(Math.Pow(2, retryCount + 1));
            _ = Task.Delay(backoff, ct).ContinueWith(
                async _ => await PublishAsync(channel, RoutingKey, ea.Body.ToArray(), retryCount + 1, ct),
                ct,
                TaskContinuationOptions.OnlyOnRanToCompletion,
                TaskScheduler.Default);
        }
    }

    private async Task PublishAsync(IChannel channel, string routingKey, byte[] body, int retryCount, CancellationToken ct)
    {
        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            Headers = new Dictionary<string, object?> { ["x-retry-count"] = retryCount }
        };
        await channel.BasicPublishAsync(_options.ExchangeName, routingKey, mandatory: false, properties, body, ct);
    }

    private static int GetRetryCount(IReadOnlyBasicProperties properties)
    {
        if (properties.Headers is not null &&
            properties.Headers.TryGetValue("x-retry-count", out var value) &&
            value is not null)
        {
            return value switch
            {
                int i => i,
                byte[] bytes => int.Parse(Encoding.UTF8.GetString(bytes)),
                _ => 0
            };
        }
        return 0;
    }
}
