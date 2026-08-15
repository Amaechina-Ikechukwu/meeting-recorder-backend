using MeetingRecorder.Application.Abstractions;
using MeetingRecorder.Domain.Entities;

namespace MeetingRecorder.Infrastructure.SpeechToText;

/// <summary>Groups Deepgram's word-level output into transcript segments, splitting on
/// sentence-ending punctuation, a >700ms gap between words (a natural pause), or a
/// Deepgram speaker change. Keeping speaker changes as hard boundaries ensures the
/// merge stage never has to guess which words belong to which voice.</summary>
internal class DeepgramTranscriptionEngine(DeepgramClient client) : ITranscriptionEngine
{
    private static readonly TimeSpan PauseThreshold = TimeSpan.FromMilliseconds(700);

    public async Task<IReadOnlyList<TranscriptSegment>> TranscribeAsync(
        string storageKey, string contentType, CancellationToken ct = default)
    {
        var words = await client.TranscribeWordsAsync(storageKey, contentType, ct);
        if (words.Count == 0)
            return [];

        var segments = new List<TranscriptSegment>();
        var buffer = new List<DeepgramWord>();

        foreach (var word in words)
        {
            if (buffer.Count > 0)
            {
                var gap = TimeSpan.FromSeconds(word.Start - buffer[^1].End);
                var previousEndsSentence = buffer[^1].PunctuatedWord?.TrimEnd() is { Length: > 0 } pw &&
                                            (pw.EndsWith('.') || pw.EndsWith('?') || pw.EndsWith('!'));

                var speakerChanged = word.Speaker != buffer[^1].Speaker;

                if (gap > PauseThreshold || previousEndsSentence || speakerChanged)
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

    private static TranscriptSegment BuildSegment(List<DeepgramWord> words) => new()
    {
        Id = Guid.NewGuid().ToString("n"),
        TranscriptId = string.Empty,
        SpeakerId = null,
        Text = string.Join(' ', words.Select(w => w.PunctuatedWord ?? w.Word)),
        StartMs = (long)(words[0].Start * 1000),
        EndMs = (long)(words[^1].End * 1000),
        Confidence = words.Average(w => w.Confidence)
    };
}
