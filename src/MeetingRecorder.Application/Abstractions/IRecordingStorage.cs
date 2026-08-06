namespace MeetingRecorder.Application.Abstractions;

public interface IRecordingStorage
{
    Task<SignedUploadUrl> CreateUploadUrlAsync(string storageKey, string contentType, CancellationToken ct = default);
    Task<Uri> CreatePlaybackUrlAsync(string storageKey, TimeSpan validFor, CancellationToken ct = default);
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct = default);
    Task WriteAsync(string storageKey, Stream content, string contentType, CancellationToken ct = default);
    Task<bool> ExistsAsync(string storageKey, CancellationToken ct = default);
    Task DeleteAsync(string storageKey, CancellationToken ct = default);
}

public record SignedUploadUrl(Uri UploadUrl, string StorageKey, DateTimeOffset ExpiresAt);
