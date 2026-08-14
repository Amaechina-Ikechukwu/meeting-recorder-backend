using MeetingRecorder.Domain.Entities;
using MeetingRecorder.Domain.Enums;
using MeetingRecorder.Domain.ValueObjects;

namespace MeetingRecorder.Application.Abstractions;

public interface IRecordingRepository
{
    Task<Recording?> GetByIdAsync(string meetingId, string recordingId, CancellationToken ct = default);
    Task CreateAsync(Recording recording, CancellationToken ct = default);
    Task UpdateAsync(Recording recording, CancellationToken ct = default);

    /// <summary>Sets only the TranscriptionReady flag. The transcription and diarization
    /// workers update the same recording concurrently, so each must write only the
    /// fields it owns — a full-document write would overwrite the other stage's result.</summary>
    Task MarkTranscriptionReadyAsync(string meetingId, string recordingId, CancellationToken ct = default);

    /// <summary>Sets only the speaker turns and the DiarizationReady flag. See
    /// <see cref="MarkTranscriptionReadyAsync"/> for why this must not write other fields.</summary>
    Task SaveDiarizationResultAsync(string meetingId, string recordingId, IReadOnlyList<SpeakerTurn> turns, CancellationToken ct = default);

    /// <summary>Sets only the recording status.</summary>
    Task UpdateStatusAsync(string meetingId, string recordingId, RecordingStatus status, CancellationToken ct = default);
}
