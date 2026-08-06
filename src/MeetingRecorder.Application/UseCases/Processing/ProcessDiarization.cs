using MeetingRecorder.Application.Abstractions;
using MeetingRecorder.Application.Messaging;
using MeetingRecorder.Domain.Entities;

namespace MeetingRecorder.Application.UseCases.Processing;

/// <summary>Diarization Worker: runs speaker segmentation over the uploaded recording,
/// independently and in parallel with transcription.</summary>
public class ProcessDiarization(
    IRecordingRepository recordings,
    IDiarizationEngine diarizationEngine,
    IMessagePublisher publisher)
{
    public async Task ExecuteAsync(RecordingUploadedMessage message, CancellationToken ct = default)
    {
        var recording = await recordings.GetByIdAsync(message.MeetingId, message.RecordingId, ct)
            ?? throw new Exceptions.NotFoundException(nameof(Recording), message.RecordingId);

        var turns = await diarizationEngine.DiarizeAsync(message.StorageKey, message.ContentType, ct);

        recording.SpeakerTurns = [.. turns];
        recording.DiarizationReady = true;
        await recordings.UpdateAsync(recording, ct);

        await publisher.PublishAsync(
            QueueNames.DiarizationCompleted,
            new DiarizationCompletedMessage(message.MeetingId, message.RecordingId),
            ct);
    }
}
