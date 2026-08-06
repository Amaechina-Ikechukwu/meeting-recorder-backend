using System.Text;
using System.Text.Json;
using MeetingRecorder.Application.Abstractions;
using MeetingRecorder.Infrastructure.Options;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace MeetingRecorder.Infrastructure.Messaging;

public class RabbitMqMessagePublisher(IConnection connection, IOptions<RabbitMqOptions> options) : IMessagePublisher
{
    private readonly RabbitMqOptions _options = options.Value;

    public async Task PublishAsync<TMessage>(string routingKey, TMessage message, CancellationToken ct = default)
        where TMessage : class
    {
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
        await channel.ExchangeDeclareAsync(_options.ExchangeName, ExchangeType.Topic, durable: true, cancellationToken: ct);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var properties = new BasicProperties { Persistent = true, ContentType = "application/json" };

        await channel.BasicPublishAsync(_options.ExchangeName, routingKey, mandatory: false, properties, body, ct);
    }
}
