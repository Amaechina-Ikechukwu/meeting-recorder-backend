using MeetingRecorder.Application.Abstractions;
using MeetingRecorder.Application.Dtos;
using MeetingRecorder.Application.Exceptions;
using MeetingRecorder.Domain.Entities;

namespace MeetingRecorder.Application.UseCases.Recordings;

public class GetUploadUrl(IMeetingRepository meetings, IRecordingRepository recordings, IRecordingStorage storage)
{
    public async Task<GetUploadUrlResponse> ExecuteAsync(
        string requesterId, string meetingId, GetUploadUrlRequest request, CancellationToken ct = default)
    {
        var meeting = await meetings.GetByIdAsync(meetingId, ct)
            ?? throw new NotFoundException(nameof(Meeting), meetingId);

        if (meeting.OwnerId != requesterId)
            throw new ForbiddenAccessException();

        var recordingId = Guid.NewGuid().ToString("n");
        var extension = request.FileExtension.TrimStart('.');
        var storageKey = $"recordings/{meetingId}/{recordingId}/audio.{extension}";

        var signed = await storage.CreateUploadUrlAsync(storageKey, request.ContentType, ct);

        var recording = new Recording
        {
            Id = recordingId,
            MeetingId = meetingId,
            StorageKey = storageKey,
            ContentType = request.ContentType,
            Status = Domain.Enums.RecordingStatus.Uploading
        };
        await recordings.CreateAsync(recording, ct);

        return new GetUploadUrlResponse(recordingId, signed.UploadUrl, signed.StorageKey, signed.ExpiresAt);
    }
}
