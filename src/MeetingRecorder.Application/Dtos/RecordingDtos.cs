namespace MeetingRecorder.Application.Dtos;

public record AudioUrlResponse(Uri Url, DateTimeOffset ExpiresAt);

public record UploadRecordingResponse(string RecordingId, string StorageKey);
