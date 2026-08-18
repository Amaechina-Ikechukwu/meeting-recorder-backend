namespace MeetingRecorder.Application.Abstractions;

/// <summary>
/// One transcribed span of audio, with the speaker the provider attributed it to.
/// </summary>
/// <param name="SpeakerLabel">
/// Provider-assigned label such as "Speaker 1", or null when the provider returned no
/// speaker attribution for the span.
/// </param>
public record TranscribedSegment(
    string Text,
    long StartMs,
    long EndMs,
    double Confidence,
    string? SpeakerLabel);

public interface ITranscriptionEngine
{
    /// <summary>
    /// Transcribes the audio at the given storage key into segments with timestamps (ms)
    /// relative to the start of the recording, each attributed to a speaker.
    /// </summary>
    Task<IReadOnlyList<TranscribedSegment>> TranscribeAsync(
        string storageKey,
        string contentType,
        CancellationToken ct = default);
}
