using MeetingRecorder.Domain.Enums;

namespace MeetingRecorder.Domain.Entities;

public class Recording
{
    public required string Id { get; init; }
    public required string MeetingId { get; init; }
    public string StorageKey { get; set; } = string.Empty;
    public string ContentType { get; set; } = "audio/webm";
    public long DurationMs { get; set; }
    public RecordingStatus Status { get; set; } = RecordingStatus.Uploading;
    public bool TranscriptionReady { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
