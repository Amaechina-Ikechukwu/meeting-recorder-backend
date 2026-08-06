namespace MeetingRecorder.Domain.ValueObjects;

public record SpeakerTurn(long StartMs, long EndMs, string SpeakerLabel);
