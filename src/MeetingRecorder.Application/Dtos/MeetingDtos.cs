using MeetingRecorder.Domain.Enums;

namespace MeetingRecorder.Application.Dtos;

public record CreateMeetingRequest(string Title, List<string>? ParticipantHints);

public record MeetingDto(
    string Id,
    string OwnerId,
    string Title,
    MeetingStatus Status,
    FailureReason FailureReason,
    string? FailureMessage,
    DateTimeOffset CreatedAt,
    List<string> ParticipantHints);

public record MeetingStatusDto(string Id, MeetingStatus Status, FailureReason FailureReason, string? FailureMessage);
