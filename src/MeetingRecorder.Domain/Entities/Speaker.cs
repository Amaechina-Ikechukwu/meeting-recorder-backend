namespace MeetingRecorder.Domain.Entities;

public class Speaker
{
    public required string Id { get; init; }
    public required string MeetingId { get; init; }
    public string Label { get; set; } = string.Empty;
    public double TotalSpeakingMs { get; set; }
}
