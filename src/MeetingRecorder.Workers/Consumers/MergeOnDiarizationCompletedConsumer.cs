using System.Text.Json;
using MeetingRecorder.Application.Messaging;
using MeetingRecorder.Application.UseCases.Processing;
using MeetingRecorder.Domain.Enums;
using MeetingRecorder.Infrastructure.Options;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace MeetingRecorder.Workers.Consumers;

/// <summary>Merge Worker (diarization-completed trigger). See
/// MergeOnTranscriptionCompletedConsumer for why two consumers share one use case.</summary>
public class MergeOnDiarizationCompletedConsumer(
    IConnection connection,
    IOptions<RabbitMqOptions> options,
    ILogger<MergeOnDiarizationCompletedConsumer> logger,
    IServiceScopeFactory scopeFactory)
    : RabbitMqConsumerBackgroundService<DiarizationCompletedMessage>(connection, options, logger)
{
    protected override string RoutingKey => QueueNames.DiarizationCompleted;
    protected override string QueueName => "merge." + QueueNames.DiarizationCompleted;

    protected override async Task HandleAsync(DiarizationCompletedMessage message, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<MergeTranscriptAndDiarization>();
        await useCase.ExecuteAsync(message.MeetingId, message.RecordingId, ct);
    }

    protected override async Task OnExhaustedAsync(byte[] body, CancellationToken ct)
    {
        var message = JsonSerializer.Deserialize<DiarizationCompletedMessage>(body);
        if (message is null)
            return;

        using var scope = scopeFactory.CreateScope();
        var markFailed = scope.ServiceProvider.GetRequiredService<MarkMeetingFailed>();
        await markFailed.ExecuteAsync(message.MeetingId, FailureReason.MergeFailed, "Merging transcript and diarization failed after multiple attempts.", ct);
    }
}
