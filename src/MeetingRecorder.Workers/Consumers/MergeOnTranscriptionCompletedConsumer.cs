using System.Text.Json;
using MeetingRecorder.Application.Messaging;
using MeetingRecorder.Application.UseCases.Processing;
using MeetingRecorder.Domain.Enums;
using MeetingRecorder.Infrastructure.Options;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace MeetingRecorder.Workers.Consumers;

/// <summary>Merge Worker (transcription-completed trigger). The use case itself checks
/// both TranscriptionReady and DiarizationReady and no-ops until both stages have landed.</summary>
public class MergeOnTranscriptionCompletedConsumer(
    IConnection connection,
    IOptions<RabbitMqOptions> options,
    ILogger<MergeOnTranscriptionCompletedConsumer> logger,
    IServiceScopeFactory scopeFactory)
    : RabbitMqConsumerBackgroundService<TranscriptionCompletedMessage>(connection, options, logger)
{
    protected override string RoutingKey => QueueNames.TranscriptionCompleted;
    protected override string QueueName => "merge." + QueueNames.TranscriptionCompleted;

    protected override async Task HandleAsync(TranscriptionCompletedMessage message, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<MergeTranscriptAndDiarization>();
        await useCase.ExecuteAsync(message.MeetingId, message.RecordingId, ct);
    }

    protected override async Task OnExhaustedAsync(byte[] body, CancellationToken ct)
    {
        var message = JsonSerializer.Deserialize<TranscriptionCompletedMessage>(body);
        if (message is null)
            return;

        using var scope = scopeFactory.CreateScope();
        var markFailed = scope.ServiceProvider.GetRequiredService<MarkMeetingFailed>();
        await markFailed.ExecuteAsync(message.MeetingId, FailureReason.MergeFailed, "Merging transcript and diarization failed after multiple attempts.", ct);
    }
}
