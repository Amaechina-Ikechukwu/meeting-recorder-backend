using MeetingRecorder.Domain.Entities;

namespace MeetingRecorder.Application.Abstractions;

public interface ITranscriptionEngine
{
    /// <summary>
    /// Transcribes the audio at the given storage key into raw, speaker-less segments
    /// with timestamps (ms) relative to the start of the recording.
    /// </summary>
    Task<IReadOnlyList<TranscriptSegment>> TranscribeAsync(
        string storageKey,
        string contentType,
        CancellationToken ct = default);
}
