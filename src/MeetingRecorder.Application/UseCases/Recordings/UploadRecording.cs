using MeetingRecorder.Application.Abstractions;
using MeetingRecorder.Application.Dtos;
using MeetingRecorder.Application.Exceptions;
using MeetingRecorder.Application.Messaging;
using MeetingRecorder.Application.UseCases.Processing;
using MeetingRecorder.Domain.Entities;
using MeetingRecorder.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MeetingRecorder.Application.UseCases.Recordings;

/// <summary>
/// Accepts recording bytes through the API, stores them, and transcribes them in the same
/// request — GetUploadUrl + client PUT + CompleteUpload + the processing worker, collapsed
/// into one call.
/// </summary>
/// <remarks>
/// <para>
/// Browsers cannot PUT to a signed GCS URL unless the bucket publishes its own CORS policy,
/// which made the direct-upload path depend on bucket configuration the API does not own.
/// Routing the bytes through here works against any bucket.
/// </para>
/// <para>
/// Transcription runs inline rather than via a queued worker. There is no Workers host
/// deployed, so a published message would have no consumer and the meeting would sit in
/// Processing forever. Running it here also drops the broker from the request path.
/// The cost is that the caller waits for the STT round-trip.
/// </para>
/// </remarks>
public class UploadRecording(
    IMeetingRepository meetings,
    IRecordingRepository recordings,
    IRecordingStorage storage,
    ProcessRecordingUploaded processRecording,
    IMeetingNotifier notifier,
    ILogger<UploadRecording> logger)
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
        // be transcribed as though it were complete.
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

        try
        {
            await processRecording.ExecuteAsync(
                new RecordingUploadedMessage(meetingId, recordingId, storageKey, contentType), ct);
        }
        catch (Exception ex)
        {
            // The audio is stored and the recording row exists, so the upload itself
            // succeeded. Report the meeting as Failed and let the caller retry processing
            // rather than failing the upload and losing the recording.
            logger.LogError(ex, "Transcription failed for meeting {MeetingId}", meetingId);

            meeting.Status = MeetingStatus.Failed;
            meeting.FailureReason = FailureReason.TranscriptionFailed;
            meeting.FailureMessage = ex.Message;
            await meetings.UpdateAsync(meeting, ct);
            await notifier.NotifyStatusChangedAsync(meetingId, MeetingStatus.Failed, ct);
        }

        return new UploadRecordingResponse(recordingId, storageKey);
    }
}
