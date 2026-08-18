using MeetingRecorder.Application.Abstractions;
using MeetingRecorder.Application.Exceptions;
using MeetingRecorder.Application.Messaging;
using MeetingRecorder.Application.UseCases.Processing;
using MeetingRecorder.Domain.Entities;
using MeetingRecorder.Domain.Enums;

namespace MeetingRecorder.Application.UseCases.Meetings;

/// <summary>Re-runs transcription for a failed meeting, in the request, as the upload does.</summary>
public class RetryMeeting(
    IMeetingRepository meetings,
    IRecordingRepository recordings,
    ProcessRecordingUploaded processRecording)
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
        recording.Status = RecordingStatus.Uploaded;
        await recordings.UpdateAsync(recording, ct);

        meeting.Status = MeetingStatus.Processing;
        meeting.FailureReason = FailureReason.None;
        meeting.FailureMessage = null;
        await meetings.UpdateAsync(meeting, ct);

        await processRecording.ExecuteAsync(
            new RecordingUploadedMessage(meeting.Id, recording.Id, recording.StorageKey, recording.ContentType), ct);
    }
}
