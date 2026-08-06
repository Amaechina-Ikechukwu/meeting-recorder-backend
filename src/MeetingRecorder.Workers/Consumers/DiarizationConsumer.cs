using System.Text.Json;
using MeetingRecorder.Application.Messaging;
using MeetingRecorder.Application.UseCases.Processing;
using MeetingRecorder.Domain.Enums;
using MeetingRecorder.Infrastructure.Options;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace MeetingRecorder.Workers.Consumers;

public class DiarizationConsumer(
    IConnection connection,
    IOptions<RabbitMqOptions> options,
    ILogger<DiarizationConsumer> logger,
    IServiceScopeFactory scopeFactory)
    : RabbitMqConsumerBackgroundService<RecordingUploadedMessage>(connection, options, logger)
{
    protected override string RoutingKey => QueueNames.RecordingUploaded;
    protected override string QueueName => "diarization." + QueueNames.RecordingUploaded;

    protected override async Task HandleAsync(RecordingUploadedMessage message, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<ProcessDiarization>();
        await useCase.ExecuteAsync(message, ct);
    }

    protected override async Task OnExhaustedAsync(byte[] body, CancellationToken ct)
    {
        var message = JsonSerializer.Deserialize<RecordingUploadedMessage>(body);
        if (message is null)
            return;

        using var scope = scopeFactory.CreateScope();
        var markFailed = scope.ServiceProvider.GetRequiredService<MarkMeetingFailed>();
        await markFailed.ExecuteAsync(message.MeetingId, FailureReason.DiarizationFailed, "Diarization failed after multiple attempts.", ct);
    }
}
