using MeetingRecorder.Domain.Entities;
using MeetingRecorder.Domain.Enums;

namespace MeetingRecorder.Application.Abstractions;

public interface IRecordingRepository
{
    Task<Recording?> GetByIdAsync(string meetingId, string recordingId, CancellationToken ct = default);
    Task CreateAsync(Recording recording, CancellationToken ct = default);
    Task UpdateAsync(Recording recording, CancellationToken ct = default);

    /// <summary>Sets only the TranscriptionReady flag, leaving the rest of the document
    /// untouched.</summary>
    Task MarkTranscriptionReadyAsync(string meetingId, string recordingId, CancellationToken ct = default);

    /// <summary>Sets only the recording status.</summary>
    Task UpdateStatusAsync(string meetingId, string recordingId, RecordingStatus status, CancellationToken ct = default);
}
