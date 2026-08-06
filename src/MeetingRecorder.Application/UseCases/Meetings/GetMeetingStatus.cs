using MeetingRecorder.Application.Abstractions;
using MeetingRecorder.Application.Dtos;
using MeetingRecorder.Application.Exceptions;
using MeetingRecorder.Domain.Entities;

namespace MeetingRecorder.Application.UseCases.Meetings;

public class GetMeetingStatus(IMeetingRepository meetings)
{
    public async Task<MeetingStatusDto> ExecuteAsync(string requesterId, string meetingId, CancellationToken ct = default)
    {
        var meeting = await meetings.GetByIdAsync(meetingId, ct)
            ?? throw new NotFoundException(nameof(Meeting), meetingId);

        if (meeting.OwnerId != requesterId)
            throw new ForbiddenAccessException();

        return meeting.ToStatusDto();
    }
}
