namespace MeetingRecorder.Domain.Entities;

public class Transcript
{
    public required string Id { get; init; }
    public required string MeetingId { get; init; }
    public required string RecordingId { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public List<TranscriptSegment> Segments { get; set; } = [];
}
