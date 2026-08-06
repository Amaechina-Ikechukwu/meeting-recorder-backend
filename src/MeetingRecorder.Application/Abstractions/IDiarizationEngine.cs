using MeetingRecorder.Domain.ValueObjects;

namespace MeetingRecorder.Application.Abstractions;

public interface IDiarizationEngine
{
    /// <summary>
    /// Produces anonymous speaker turns ("Speaker 1", "Speaker 2", ...) for the audio
    /// at the given storage key.
    /// </summary>
    Task<IReadOnlyList<SpeakerTurn>> DiarizeAsync(
        string storageKey,
        string contentType,
        CancellationToken ct = default);
}
