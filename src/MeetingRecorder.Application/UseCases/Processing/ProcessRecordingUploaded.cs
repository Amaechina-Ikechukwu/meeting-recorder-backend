using MeetingRecorder.Application.Abstractions;
using MeetingRecorder.Application.Messaging;
using MeetingRecorder.Domain.Entities;

namespace MeetingRecorder.Application.UseCases.Processing;

/// <summary>Transcription Worker: runs STT over the uploaded recording and stores raw,
/// speaker-less segments, pushing each one to subscribed clients as it lands.</summary>
public class ProcessRecordingUploaded(
    IRecordingRepository recordings,
    ITranscriptRepository transcripts,
    ITranscriptionEngine transcriptionEngine,
    IMeetingNotifier notifier,
    IMessagePublisher publisher)
{
    public async Task ExecuteAsync(RecordingUploadedMessage message, CancellationToken ct = default)
    {
        var recording = await recordings.GetByIdAsync(message.MeetingId, message.RecordingId, ct)
            ?? throw new Exceptions.NotFoundException(nameof(Recording), message.RecordingId);

        var rawSegments = await transcriptionEngine.TranscribeAsync(message.StorageKey, message.ContentType, ct);

        var transcript = await transcripts.GetByMeetingIdAsync(message.MeetingId, ct)
            ?? new Transcript
            {
                Id = Guid.NewGuid().ToString("n"),
                MeetingId = message.MeetingId,
                RecordingId = message.RecordingId
            };

        transcript.Segments = [.. rawSegments.Select(s => new TranscriptSegment
        {
            Id = s.Id,
            TranscriptId = transcript.Id,
            SpeakerId = s.SpeakerId,
            Text = s.Text,
            StartMs = s.StartMs,
            EndMs = s.EndMs,
            Confidence = s.Confidence
        })];
        await transcripts.SaveAsync(transcript, ct);

        foreach (var segment in transcript.Segments.OrderBy(s => s.StartMs))
            await notifier.NotifyTranscriptSegmentReadyAsync(message.MeetingId, segment, ct);

        recording.TranscriptionReady = true;
        await recordings.UpdateAsync(recording, ct);

        await publisher.PublishAsync(
            QueueNames.TranscriptionCompleted,
            new TranscriptionCompletedMessage(message.MeetingId, message.RecordingId),
            ct);
    }
}
