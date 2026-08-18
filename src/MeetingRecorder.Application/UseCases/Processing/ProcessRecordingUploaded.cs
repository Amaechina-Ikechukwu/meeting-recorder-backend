using MeetingRecorder.Application.Abstractions;
using MeetingRecorder.Application.Messaging;
using MeetingRecorder.Domain.Entities;
using MeetingRecorder.Domain.Enums;

namespace MeetingRecorder.Application.UseCases.Processing;

/// <summary>
/// Transcribes an uploaded recording, attributes each segment to a speaker, and completes
/// the meeting.
/// </summary>
/// <remarks>
/// This is the whole processing pipeline, in one pass. Diarization is not a separate stage:
/// the provider returns a speaker on the same words that carry the text, so a second
/// transcription of the same audio — which is what the old diarization worker did — buys
/// nothing. Collapsing it halves both the STT bill and the wall-clock time, and removes the
/// two-workers-must-both-finish gate that used to stand between a recording and Ready.
/// </remarks>
public class ProcessRecordingUploaded(
    IMeetingRepository meetings,
    IRecordingRepository recordings,
    ITranscriptRepository transcripts,
    ISpeakerRepository speakers,
    ITranscriptionEngine transcriptionEngine,
    IMeetingNotifier notifier)
{
    public async Task ExecuteAsync(RecordingUploadedMessage message, CancellationToken ct = default)
    {
        _ = await recordings.GetByIdAsync(message.MeetingId, message.RecordingId, ct)
            ?? throw new Exceptions.NotFoundException(nameof(Recording), message.RecordingId);

        var rawSegments = await transcriptionEngine.TranscribeAsync(message.StorageKey, message.ContentType, ct);

        var transcript = await transcripts.GetByMeetingIdAsync(message.MeetingId, ct)
            ?? new Transcript
            {
                Id = Guid.NewGuid().ToString("n"),
                MeetingId = message.MeetingId,
                RecordingId = message.RecordingId
            };

        // Reuse any speaker already stored for this meeting so a label the user renamed
        // survives a reprocess, and so retries do not create duplicates.
        var existing = await speakers.GetByMeetingIdAsync(message.MeetingId, ct);
        var speakersByLabel = existing.ToDictionary(s => s.Label, s => s);

        foreach (var label in rawSegments.Select(s => s.SpeakerLabel).OfType<string>().Distinct())
        {
            if (speakersByLabel.ContainsKey(label))
                continue;

            speakersByLabel[label] = new Speaker
            {
                Id = Guid.NewGuid().ToString("n"),
                MeetingId = message.MeetingId,
                Label = label
            };
        }

        transcript.Segments = [.. rawSegments.Select(s => new TranscriptSegment
        {
            Id = Guid.NewGuid().ToString("n"),
            TranscriptId = transcript.Id,
            SpeakerId = s.SpeakerLabel is { } label ? speakersByLabel[label].Id : null,
            Text = s.Text,
            StartMs = s.StartMs,
            EndMs = s.EndMs,
            Confidence = s.Confidence
        })];
        await transcripts.SaveAsync(transcript, ct);

        // Recompute speaking time from this transcript rather than accumulating, so a retry
        // does not double-count. The upserts do not depend on each other, so overlap them
        // instead of paying for the Firestore round trips end to end.
        foreach (var speaker in speakersByLabel.Values)
        {
            speaker.TotalSpeakingMs = transcript.Segments
                .Where(seg => seg.SpeakerId == speaker.Id)
                .Sum(seg => seg.EndMs - seg.StartMs);
        }
        await Task.WhenAll(speakersByLabel.Values.Select(s => speakers.UpsertAsync(s, ct)));

        await notifier.NotifyTranscriptSegmentsReadyAsync(
            message.MeetingId, [.. transcript.Segments.OrderBy(s => s.StartMs)], ct);

        await recordings.MarkTranscriptionReadyAsync(message.MeetingId, message.RecordingId, ct);
        await recordings.UpdateStatusAsync(message.MeetingId, message.RecordingId, RecordingStatus.Processed, ct);

        var meeting = await meetings.GetByIdAsync(message.MeetingId, ct)
            ?? throw new Exceptions.NotFoundException(nameof(Meeting), message.MeetingId);
        meeting.Status = MeetingStatus.Ready;
        meeting.FailureReason = FailureReason.None;
        meeting.FailureMessage = null;
        await meetings.UpdateAsync(meeting, ct);

        await notifier.NotifyStatusChangedAsync(message.MeetingId, MeetingStatus.Ready, ct);
    }
}
