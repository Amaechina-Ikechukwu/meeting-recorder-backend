using MeetingRecorder.Domain.Entities;

namespace MeetingRecorder.Application.Abstractions;

public interface ISpeakerRepository
{
    Task<IReadOnlyList<Speaker>> GetByMeetingIdAsync(string meetingId, CancellationToken ct = default);
    Task<Speaker?> GetByIdAsync(string meetingId, string speakerId, CancellationToken ct = default);
    Task UpsertAsync(Speaker speaker, CancellationToken ct = default);
}
