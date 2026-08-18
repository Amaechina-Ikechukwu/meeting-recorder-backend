using MeetingRecorder.Application.Abstractions;
using MeetingRecorder.Application.Dtos;
using MeetingRecorder.Application.Exceptions;
using MeetingRecorder.Application.Messaging;
using MeetingRecorder.Domain.Entities;
using MeetingRecorder.Domain.Enums;

namespace MeetingRecorder.Application.UseCases.Recordings;

/// <summary>
/// Accepts recording bytes through the API and stores them, then starts the async
/// transcription pipeline — GetUploadUrl + client PUT + CompleteUpload in a single call.
/// </summary>
/// <remarks>
/// Browsers cannot PUT to a signed GCS URL unless the bucket publishes its own CORS policy,
/// which makes the direct-upload path depend on bucket configuration the API does not own.
/// Routing the bytes through here keeps uploads working against any bucket.
/// </remarks>
public class UploadRecording(
    IMeetingRepository meetings,
    IRecordingRepository recordings,
    IRecordingStorage storage,
    IMessagePublisher publisher,
    IMeetingNotifier notifier)
{
    public async Task<UploadRecordingResponse> ExecuteAsync(
        string requesterId,
        string meetingId,
        Stream content,
        string contentType,
        string? fileExtension,
        long durationMs,
        CancellationToken ct = default)
    {
        var meeting = await meetings.GetByIdAsync(meetingId, ct)
            ?? throw new NotFoundException(nameof(Meeting), meetingId);

        if (meeting.OwnerId != requesterId)
            throw new ForbiddenAccessException();

        if (string.IsNullOrWhiteSpace(contentType))
            throw new ValidationFailedException(["A Content-Type describing the audio is required."]);

        if (durationMs < 0)
            throw new ValidationFailedException(["durationMs cannot be negative."]);

        var recordingId = Guid.NewGuid().ToString("n");
        var extension = string.IsNullOrWhiteSpace(fileExtension) ? "webm" : fileExtension.TrimStart('.');
        var storageKey = $"recordings/{meetingId}/{recordingId}/audio.{extension}";

        await storage.WriteAsync(storageKey, content, contentType, ct);

        // Confirm the object actually landed: a truncated or aborted upload would otherwise
        // be handed to the transcription worker as though it were complete.
        if (!await storage.ExistsAsync(storageKey, ct))
            throw new ValidationFailedException(["The upload did not reach storage."]);

        var recording = new Recording
        {
            Id = recordingId,
            MeetingId = meetingId,
            StorageKey = storageKey,
            ContentType = contentType,
            DurationMs = durationMs,
            Status = RecordingStatus.Uploaded
        };
        await recordings.CreateAsync(recording, ct);

        meeting.Status = MeetingStatus.Processing;
        await meetings.UpdateAsync(meeting, ct);
        await notifier.NotifyStatusChangedAsync(meetingId, MeetingStatus.Processing, ct);

        await publisher.PublishAsync(
            QueueNames.RecordingUploaded,
            new RecordingUploadedMessage(meetingId, recordingId, storageKey, contentType),
            ct);

        return new UploadRecordingResponse(recordingId, storageKey);
    }
}
