using MeetingRecorder.Domain.Entities;

namespace MeetingRecorder.Application.Abstractions;

public interface IMeetingRepository
{
    Task<Meeting?> GetByIdAsync(string meetingId, CancellationToken ct = default);
    Task<IReadOnlyList<Meeting>> GetByOwnerAsync(string ownerId, CancellationToken ct = default);
    Task CreateAsync(Meeting meeting, CancellationToken ct = default);
    Task UpdateAsync(Meeting meeting, CancellationToken ct = default);
}
