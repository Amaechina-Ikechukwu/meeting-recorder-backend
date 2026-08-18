using MeetingRecorder.Application.Abstractions;

namespace MeetingRecorder.Infrastructure.SpeechToText;

/// <summary>Groups Deepgram's word-level output into transcript segments, splitting on
/// sentence-ending punctuation, a >700ms gap between words (a natural pause), or a
/// speaker change.</summary>
/// <remarks>
/// Deepgram returns a speaker index on every word of the same response that carries the
/// text, so speaker attribution costs nothing beyond this one call. Deriving it here is
/// what lets the pipeline drop the second, identical transcription pass that used to run
/// purely to recover these labels.
/// </remarks>
internal class DeepgramTranscriptionEngine(DeepgramClient client) : ITranscriptionEngine
{
    private static readonly TimeSpan PauseThreshold = TimeSpan.FromMilliseconds(700);

    public async Task<IReadOnlyList<TranscribedSegment>> TranscribeAsync(
        string storageKey, string contentType, CancellationToken ct = default)
    {
        var words = await client.TranscribeWordsAsync(storageKey, contentType, ct);
        if (words.Count == 0)
            return [];

        var segments = new List<TranscribedSegment>();
        var buffer = new List<DeepgramWord>();

        foreach (var word in words)
        {
            if (buffer.Count > 0)
            {
                var gap = TimeSpan.FromSeconds(word.Start - buffer[^1].End);
                var previousEndsSentence = buffer[^1].PunctuatedWord?.TrimEnd() is { Length: > 0 } pw &&
                                            (pw.EndsWith('.') || pw.EndsWith('?') || pw.EndsWith('!'));

                // A speaker change is a hard boundary: never merge two voices into one segment.
                var speakerChanged = word.Speaker != buffer[^1].Speaker;

                if (speakerChanged || gap > PauseThreshold || previousEndsSentence)
                {
                    segments.Add(BuildSegment(buffer));
                    buffer.Clear();
                }
            }

            buffer.Add(word);
        }

        if (buffer.Count > 0)
            segments.Add(BuildSegment(buffer));

        return segments;
    }

    private static TranscribedSegment BuildSegment(List<DeepgramWord> words) => new(
        Text: string.Join(' ', words.Select(w => w.PunctuatedWord ?? w.Word)),
        StartMs: (long)(words[0].Start * 1000),
        EndMs: (long)(words[^1].End * 1000),
        Confidence: words.Average(w => w.Confidence),
        SpeakerLabel: words[0].Speaker is { } s ? $"Speaker {s + 1}" : null);
}
