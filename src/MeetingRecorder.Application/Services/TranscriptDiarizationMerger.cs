using MeetingRecorder.Domain.Entities;
using MeetingRecorder.Domain.ValueObjects;

namespace MeetingRecorder.Application.Services;

/// <summary>
/// Aligns diarization speaker turns with raw (speaker-less) transcript segments by
/// timestamp overlap. The transcription engine normally makes speaker changes hard
/// segment boundaries. If two independently-produced Deepgram responses differ by a
/// few milliseconds at a boundary, the merger assigns the segment to the speaker
/// with the greatest overlap instead of duplicating its text under each speaker.
/// </summary>
public static class TranscriptDiarizationMerger
{
    public static List<TranscriptSegment> Merge(
        IReadOnlyList<TranscriptSegment> rawSegments,
        IReadOnlyList<SpeakerTurn> speakerTurns,
        Func<string, string> speakerIdForLabel)
    {
        if (speakerTurns.Count == 0)
            return [.. rawSegments];

        var orderedTurns = speakerTurns.OrderBy(t => t.StartMs).ToList();
        var result = new List<TranscriptSegment>();

        foreach (var segment in rawSegments.OrderBy(s => s.StartMs))
        {
            var overlappingTurns = orderedTurns
                .Where(t => t.StartMs < segment.EndMs && t.EndMs > segment.StartMs)
                .ToList();

            if (overlappingTurns.Count == 0)
            {
                result.Add(segment);
                continue;
            }

            if (overlappingTurns.Count == 1)
            {
                result.Add(WithSpeaker(segment, segment.StartMs, segment.EndMs, overlappingTurns[0], speakerIdForLabel));
                continue;
            }

            // Transcript segments do not retain individual word timings, so splitting
            // this text into every overlapping range would repeat the full sentence
            // for multiple speakers. A speaker boundary should already have split it
            // upstream; this is a safe fallback for tiny timing differences between
            // the parallel transcription and diarization requests.
            var dominantTurn = overlappingTurns
                .OrderByDescending(turn => Math.Min(segment.EndMs, turn.EndMs) - Math.Max(segment.StartMs, turn.StartMs))
                .ThenBy(turn => turn.StartMs)
                .First();
            result.Add(WithSpeaker(segment, segment.StartMs, segment.EndMs, dominantTurn, speakerIdForLabel));
        }

        return result;
    }

    private static TranscriptSegment WithSpeaker(
        TranscriptSegment source, long startMs, long endMs, SpeakerTurn turn, Func<string, string> speakerIdForLabel) =>
        new()
        {
            Id = startMs == source.StartMs && endMs == source.EndMs ? source.Id : Guid.NewGuid().ToString("n"),
            TranscriptId = source.TranscriptId,
            SpeakerId = speakerIdForLabel(turn.SpeakerLabel),
            Text = source.Text,
            StartMs = startMs,
            EndMs = endMs,
            Confidence = source.Confidence
        };
}
