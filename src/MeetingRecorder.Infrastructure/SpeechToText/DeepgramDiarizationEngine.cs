using MeetingRecorder.Application.Abstractions;
using MeetingRecorder.Domain.ValueObjects;

namespace MeetingRecorder.Infrastructure.SpeechToText;

/// <summary>Groups Deepgram's per-word speaker index into contiguous speaker turns.</summary>
internal class DeepgramDiarizationEngine(DeepgramClient client) : IDiarizationEngine
{
    public async Task<IReadOnlyList<SpeakerTurn>> DiarizeAsync(
        string storageKey, string contentType, CancellationToken ct = default)
    {
        // The API returns chronological words, but sort defensively before deriving
        // turns: a non-ordered response would otherwise create overlapping speaker
        // ranges and cause the transcript merger to mislabel segments.
        var words = (await client.TranscribeWordsAsync(storageKey, contentType, ct))
            .OrderBy(word => word.Start)
            .ThenBy(word => word.End)
            .ToList();
        if (words.Count == 0)
            return [];

        var turns = new List<SpeakerTurn>();
        var currentSpeaker = words[0].Speaker ?? 0;
        var turnStart = words[0].Start;
        var turnEnd = words[0].End;

        foreach (var word in words.Skip(1))
        {
            var speaker = word.Speaker ?? 0;
            if (speaker != currentSpeaker)
            {
                turns.Add(new SpeakerTurn((long)(turnStart * 1000), (long)(turnEnd * 1000), $"Speaker {currentSpeaker + 1}"));
                currentSpeaker = speaker;
                turnStart = word.Start;
            }

            turnEnd = word.End;
        }

        turns.Add(new SpeakerTurn((long)(turnStart * 1000), (long)(turnEnd * 1000), $"Speaker {currentSpeaker + 1}"));
        return turns;
    }
}
