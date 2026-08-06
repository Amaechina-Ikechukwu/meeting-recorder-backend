using MeetingRecorder.Application.Abstractions;
using MeetingRecorder.Application.Dtos;
using MeetingRecorder.Application.Exceptions;
using MeetingRecorder.Application.Messaging;
using MeetingRecorder.Domain.Entities;
using MeetingRecorder.Domain.Enums;

namespace MeetingRecorder.Application.UseCases.Recordings;

/// <summary>Confirms a direct-to-storage upload finished, marks the recording Uploaded, and
/// kicks off the async transcription/diarization pipeline.</summary>
public class CompleteUpload(
    IMeetingRepository meetings,
    IRecordingRepository recordings,
    IRecordingStorage storage,
    IMessagePublisher publisher,
    IMeetingNotifier notifier)
{
    public async Task ExecuteAsync(
        string requesterId, string meetingId, string recordingId, CompleteUploadRequest request, CancellationToken ct = default)
    {
        var meeting = await meetings.GetByIdAsync(meetingId, ct)
            ?? throw new NotFoundException(nameof(Meeting), meetingId);

        if (meeting.OwnerId != requesterId)
            throw new ForbiddenAccessException();

        var recording = await recordings.GetByIdAsync(meetingId, recordingId, ct)
            ?? throw new NotFoundException(nameof(Recording), recordingId);

        if (!await storage.ExistsAsync(recording.StorageKey, ct))
            throw new ValidationFailedException(["Uploaded object was not found in storage."]);

        recording.Status = RecordingStatus.Uploaded;
        recording.DurationMs = request.DurationMs;
        await recordings.UpdateAsync(recording, ct);

        meeting.Status = MeetingStatus.Processing;
        await meetings.UpdateAsync(meeting, ct);
        await notifier.NotifyStatusChangedAsync(meetingId, MeetingStatus.Processing, ct);

        await publisher.PublishAsync(
            QueueNames.RecordingUploaded,
            new RecordingUploadedMessage(meetingId, recordingId, recording.StorageKey, recording.ContentType),
            ct);
    }
}
