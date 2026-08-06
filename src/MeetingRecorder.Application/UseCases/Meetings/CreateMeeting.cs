using MeetingRecorder.Application.Abstractions;
using MeetingRecorder.Application.Dtos;
using MeetingRecorder.Domain.Entities;

namespace MeetingRecorder.Application.UseCases.Meetings;

public class CreateMeeting(IMeetingRepository meetings)
{
    public async Task<MeetingDto> ExecuteAsync(string ownerId, CreateMeetingRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new Exceptions.ValidationFailedException(["Title is required."]);

        var meeting = new Meeting
        {
            Id = Guid.NewGuid().ToString("n"),
            OwnerId = ownerId,
            Title = request.Title.Trim(),
            ParticipantHints = request.ParticipantHints ?? []
        };

        await meetings.CreateAsync(meeting, ct);

        return meeting.ToDto();
    }
}
