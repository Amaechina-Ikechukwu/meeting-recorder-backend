using MeetingRecorder.Domain.Entities;

namespace MeetingRecorder.Application.Abstractions;

public interface ITranscriptRepository
{
    Task<Transcript?> GetByMeetingIdAsync(string meetingId, CancellationToken ct = default);
    Task SaveAsync(Transcript transcript, CancellationToken ct = default);
    Task<IReadOnlyList<TranscriptSegment>> SearchAsync(string meetingId, string query, CancellationToken ct = default);
}
