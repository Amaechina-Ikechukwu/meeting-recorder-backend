using MeetingRecorder.Application.Abstractions;
using MeetingRecorder.Application.Messaging;
using MeetingRecorder.Application.Services;
using MeetingRecorder.Domain.Entities;
using MeetingRecorder.Domain.Enums;

namespace MeetingRecorder.Application.UseCases.Processing;

/// <summary>Merge Worker: once both transcription and diarization have completed for a
/// recording, aligns speaker turns with transcript segments and marks the meeting Ready.</summary>
public class MergeTranscriptAndDiarization(
    IMeetingRepository meetings,
    IRecordingRepository recordings,
    ITranscriptRepository transcripts,
    ISpeakerRepository speakers,
    IMessagePublisher publisher)
{
    public async Task ExecuteAsync(string meetingId, string recordingId, CancellationToken ct = default)
    {
        var recording = await recordings.GetByIdAsync(meetingId, recordingId, ct)
            ?? throw new Exceptions.NotFoundException(nameof(Recording), recordingId);

        // Only merge once both parallel stages have landed; the other completion event
        // will trigger this again and find both flags set.
        if (!recording.TranscriptionReady || !recording.DiarizationReady)
            return;

        var transcript = await transcripts.GetByMeetingIdAsync(meetingId, ct)
            ?? throw new Exceptions.NotFoundException(nameof(Transcript), meetingId);

        var existingSpeakers = await speakers.GetByMeetingIdAsync(meetingId, ct);
        var speakerIdByLabel = existingSpeakers.ToDictionary(s => s.Label, s => s.Id);

        foreach (var label in recording.SpeakerTurns.Select(t => t.SpeakerLabel).Distinct())
        {
            if (speakerIdByLabel.ContainsKey(label))
                continue;

            var speaker = new Speaker { Id = Guid.NewGuid().ToString("n"), MeetingId = meetingId, Label = label };
            await speakers.UpsertAsync(speaker, ct);
            speakerIdByLabel[label] = speaker.Id;
        }

        transcript.Segments = TranscriptDiarizationMerger.Merge(
            transcript.Segments, recording.SpeakerTurns, label => speakerIdByLabel[label]);
        await transcripts.SaveAsync(transcript, ct);

        var speakerById = existingSpeakers.ToDictionary(s => s.Id, s => s);
        foreach (var turn in recording.SpeakerTurns)
        {
            var speakerId = speakerIdByLabel[turn.SpeakerLabel];
            if (!speakerById.TryGetValue(speakerId, out var speaker))
                speakerById[speakerId] = speaker = new Speaker { Id = speakerId, MeetingId = meetingId, Label = turn.SpeakerLabel };

            speaker.TotalSpeakingMs += turn.EndMs - turn.StartMs;
        }

        foreach (var speaker in speakerById.Values)
            await speakers.UpsertAsync(speaker, ct);

        var meeting = await meetings.GetByIdAsync(meetingId, ct)
            ?? throw new Exceptions.NotFoundException(nameof(Meeting), meetingId);
        meeting.Status = MeetingStatus.Ready;
        await meetings.UpdateAsync(meeting, ct);

        recording.Status = RecordingStatus.Processed;
        await recordings.UpdateAsync(recording, ct);

        await publisher.PublishAsync(QueueNames.MeetingReady, new MeetingReadyMessage(meetingId), ct);
    }
}
