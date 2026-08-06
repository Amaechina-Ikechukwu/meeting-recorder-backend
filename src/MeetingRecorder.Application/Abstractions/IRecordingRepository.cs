using MeetingRecorder.Domain.Entities;

namespace MeetingRecorder.Application.Abstractions;

public interface IRecordingRepository
{
    Task<Recording?> GetByIdAsync(string meetingId, string recordingId, CancellationToken ct = default);
    Task CreateAsync(Recording recording, CancellationToken ct = default);
    Task UpdateAsync(Recording recording, CancellationToken ct = default);
}
