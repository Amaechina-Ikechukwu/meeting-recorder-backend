namespace MeetingRecorder.Domain.Entities;

public class TranscriptSegment
{
    public required string Id { get; init; }
    public required string TranscriptId { get; init; }
    public string? SpeakerId { get; set; }
    public string Text { get; set; } = string.Empty;
    public long StartMs { get; set; }
    public long EndMs { get; set; }
    public double Confidence { get; set; }
}
