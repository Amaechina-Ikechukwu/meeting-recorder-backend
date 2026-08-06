using MeetingRecorder.Application.Abstractions;
using MeetingRecorder.Application.Dtos;
using MeetingRecorder.Application.Exceptions;
using MeetingRecorder.Domain.Entities;

namespace MeetingRecorder.Application.UseCases.Transcripts;

public class SearchTranscript(IMeetingRepository meetings, ITranscriptRepository transcripts)
{
    public async Task<List<TranscriptSegmentDto>> ExecuteAsync(
        string requesterId, string meetingId, string query, CancellationToken ct = default)
    {
        var meeting = await meetings.GetByIdAsync(meetingId, ct)
            ?? throw new NotFoundException(nameof(Meeting), meetingId);

        if (meeting.OwnerId != requesterId)
            throw new ForbiddenAccessException();

        if (string.IsNullOrWhiteSpace(query))
            return [];

        var results = await transcripts.SearchAsync(meetingId, query, ct);

        return results
            .OrderBy(s => s.StartMs)
            .Select(s => new TranscriptSegmentDto(s.Id, s.SpeakerId, null, s.Text, s.StartMs, s.EndMs, s.Confidence))
            .ToList();
    }
}
