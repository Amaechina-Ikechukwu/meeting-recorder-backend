using MeetingRecorder.Domain.Enums;

namespace MeetingRecorder.Domain.Entities;

public class Meeting
{
    public required string Id { get; init; }
    public required string OwnerId { get; init; }
    public string Title { get; set; } = string.Empty;
    public MeetingStatus Status { get; set; } = MeetingStatus.Recording;
    public FailureReason FailureReason { get; set; } = FailureReason.None;
    public string? FailureMessage { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public List<string> ParticipantHints { get; set; } = [];
}
