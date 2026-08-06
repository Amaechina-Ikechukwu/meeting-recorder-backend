using MeetingRecorder.Application.Abstractions;
using MeetingRecorder.Application.Dtos;
using MeetingRecorder.Application.Exceptions;
using MeetingRecorder.Domain.Entities;

namespace MeetingRecorder.Application.UseCases.Transcripts;

public class GetMeetingTranscript(IMeetingRepository meetings, ITranscriptRepository transcripts, ISpeakerRepository speakers)
{
    public async Task<TranscriptDto> ExecuteAsync(string requesterId, string meetingId, CancellationToken ct = default)
    {
        var meeting = await meetings.GetByIdAsync(meetingId, ct)
            ?? throw new NotFoundException(nameof(Meeting), meetingId);

        if (meeting.OwnerId != requesterId)
            throw new ForbiddenAccessException();

        var transcript = await transcripts.GetByMeetingIdAsync(meetingId, ct)
            ?? throw new NotFoundException(nameof(Transcript), meetingId);

        var speakerList = await speakers.GetByMeetingIdAsync(meetingId, ct);
        var labelsById = speakerList.ToDictionary(s => s.Id, s => s.Label);

        var segments = transcript.Segments
            .OrderBy(s => s.StartMs)
            .Select(s => new TranscriptSegmentDto(
                s.Id,
                s.SpeakerId,
                s.SpeakerId is not null && labelsById.TryGetValue(s.SpeakerId, out var label) ? label : null,
                s.Text,
                s.StartMs,
                s.EndMs,
                s.Confidence))
            .ToList();

        return new TranscriptDto(transcript.Id, transcript.MeetingId, transcript.CreatedAt, segments);
    }
}
