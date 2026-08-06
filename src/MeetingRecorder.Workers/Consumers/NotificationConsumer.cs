using MeetingRecorder.Application.Messaging;
using MeetingRecorder.Application.UseCases.Processing;
using MeetingRecorder.Infrastructure.Options;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace MeetingRecorder.Workers.Consumers;

/// <summary>Notification Worker: emails the owner and pushes meetingStatusChanged=Ready.</summary>
public class NotificationConsumer(
    IConnection connection,
    IOptions<RabbitMqOptions> options,
    ILogger<NotificationConsumer> logger,
    IServiceScopeFactory scopeFactory)
    : RabbitMqConsumerBackgroundService<MeetingReadyMessage>(connection, options, logger)
{
    protected override string RoutingKey => QueueNames.MeetingReady;

    protected override async Task HandleAsync(MeetingReadyMessage message, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<ProcessMeetingReady>();
        await useCase.ExecuteAsync(message.MeetingId, ct);
    }
}
