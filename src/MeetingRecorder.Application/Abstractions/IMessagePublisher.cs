namespace MeetingRecorder.Application.Abstractions;

/// <summary>Publishes integration events onto the async processing bus (RabbitMQ).</summary>
public interface IMessagePublisher
{
    Task PublishAsync<TMessage>(string routingKey, TMessage message, CancellationToken ct = default)
        where TMessage : class;
}
