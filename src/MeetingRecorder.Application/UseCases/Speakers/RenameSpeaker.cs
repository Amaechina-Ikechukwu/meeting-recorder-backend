using MeetingRecorder.Application.Abstractions;
using MeetingRecorder.Application.Dtos;
using MeetingRecorder.Application.Exceptions;
using MeetingRecorder.Domain.Entities;

namespace MeetingRecorder.Application.UseCases.Speakers;

public class RenameSpeaker(IMeetingRepository meetings, ISpeakerRepository speakers)
{
    public async Task<SpeakerDto> ExecuteAsync(
        string requesterId, string meetingId, string speakerId, RenameSpeakerRequest request, CancellationToken ct = default)
    {
        var meeting = await meetings.GetByIdAsync(meetingId, ct)
            ?? throw new NotFoundException(nameof(Meeting), meetingId);

        if (meeting.OwnerId != requesterId)
            throw new ForbiddenAccessException();

        if (string.IsNullOrWhiteSpace(request.Label))
            throw new ValidationFailedException(["Label is required."]);

        var speaker = await speakers.GetByIdAsync(meetingId, speakerId, ct)
            ?? throw new NotFoundException(nameof(Speaker), speakerId);

        speaker.Label = request.Label.Trim();
        await speakers.UpsertAsync(speaker, ct);

        return new SpeakerDto(speaker.Id, speaker.MeetingId, speaker.Label, speaker.TotalSpeakingMs);
    }
}
