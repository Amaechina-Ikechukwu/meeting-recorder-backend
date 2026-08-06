using MeetingRecorder.Application.Abstractions;
using MeetingRecorder.Application.Exceptions;
using MeetingRecorder.Application.Messaging;
using MeetingRecorder.Domain.Entities;
using MeetingRecorder.Domain.Enums;

namespace MeetingRecorder.Application.UseCases.Meetings;

/// <summary>Re-publishes the recording.uploaded event for a failed meeting so processing restarts.</summary>
public class RetryMeeting(
    IMeetingRepository meetings,
    IRecordingRepository recordings,
    IMessagePublisher publisher)
{
    public async Task ExecuteAsync(string requesterId, string meetingId, string recordingId, CancellationToken ct = default)
    {
        var meeting = await meetings.GetByIdAsync(meetingId, ct)
            ?? throw new NotFoundException(nameof(Meeting), meetingId);

        if (meeting.OwnerId != requesterId)
            throw new ForbiddenAccessException();

        if (meeting.Status != MeetingStatus.Failed)
            throw new ValidationFailedException(["Meeting is not in a failed state."]);

        var recording = await recordings.GetByIdAsync(meetingId, recordingId, ct)
            ?? throw new NotFoundException("Recording", recordingId);

        recording.TranscriptionReady = false;
        recording.DiarizationReady = false;
        recording.SpeakerTurns = [];
        recording.Status = Domain.Enums.RecordingStatus.Uploaded;
        await recordings.UpdateAsync(recording, ct);

        meeting.Status = MeetingStatus.Processing;
        meeting.FailureReason = FailureReason.None;
        meeting.FailureMessage = null;
        await meetings.UpdateAsync(meeting, ct);

        await publisher.PublishAsync(
            QueueNames.RecordingUploaded,
            new RecordingUploadedMessage(meeting.Id, recording.Id, recording.StorageKey, recording.ContentType),
            ct);
    }
}
