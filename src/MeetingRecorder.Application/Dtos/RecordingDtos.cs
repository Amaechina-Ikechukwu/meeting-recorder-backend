namespace MeetingRecorder.Application.Dtos;

public record GetUploadUrlRequest(string ContentType, string FileExtension);

public record GetUploadUrlResponse(string RecordingId, Uri UploadUrl, string StorageKey, DateTimeOffset ExpiresAt);

public record CompleteUploadRequest(long DurationMs);

public record AudioUrlResponse(Uri Url, DateTimeOffset ExpiresAt);
