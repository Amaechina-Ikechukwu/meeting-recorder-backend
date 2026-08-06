using MeetingRecorder.Application.Dtos;
using MeetingRecorder.Domain.Entities;

namespace MeetingRecorder.Application.UseCases.Meetings;

public static class MeetingMappingExtensions
{
    public static MeetingDto ToDto(this Meeting meeting) => new(
        meeting.Id,
        meeting.OwnerId,
        meeting.Title,
        meeting.Status,
        meeting.FailureReason,
        meeting.FailureMessage,
        meeting.CreatedAt,
        meeting.ParticipantHints);

    public static MeetingStatusDto ToStatusDto(this Meeting meeting) => new(
        meeting.Id,
        meeting.Status,
        meeting.FailureReason,
        meeting.FailureMessage);
}
