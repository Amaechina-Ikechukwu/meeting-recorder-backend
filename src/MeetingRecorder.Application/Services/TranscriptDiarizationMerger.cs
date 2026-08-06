using MeetingRecorder.Domain.Entities;
using MeetingRecorder.Domain.ValueObjects;

namespace MeetingRecorder.Application.Services;

/// <summary>
/// Aligns diarization speaker turns with raw (speaker-less) transcript segments by
/// timestamp overlap. Segments that span a speaker change are split at the boundary.
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

            // Segment spans a speaker change: split at each turn boundary within the segment.
            foreach (var turn in overlappingTurns)
            {
                var sliceStart = Math.Max(segment.StartMs, turn.StartMs);
                var sliceEnd = Math.Min(segment.EndMs, turn.EndMs);
                if (sliceEnd <= sliceStart)
                    continue;

                result.Add(WithSpeaker(segment, sliceStart, sliceEnd, turn, speakerIdForLabel));
            }
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
