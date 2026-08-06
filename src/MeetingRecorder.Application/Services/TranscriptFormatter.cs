using System.Text;
using MeetingRecorder.Domain.Entities;

namespace MeetingRecorder.Application.Services;

/// <summary>Renders a merged transcript into plain-text caption/export formats.</summary>
public static class TranscriptFormatter
{
    public static string ToSrt(IReadOnlyList<TranscriptSegment> segments, IReadOnlyDictionary<string, string> speakerLabels)
    {
        var sb = new StringBuilder();
        var index = 1;
        foreach (var segment in segments.OrderBy(s => s.StartMs))
        {
            sb.AppendLine((index++).ToString());
            sb.AppendLine($"{FormatSrtTimestamp(segment.StartMs)} --> {FormatSrtTimestamp(segment.EndMs)}");
            sb.AppendLine(WithSpeakerPrefix(segment, speakerLabels));
            sb.AppendLine();
        }
        return sb.ToString();
    }

    public static string ToVtt(IReadOnlyList<TranscriptSegment> segments, IReadOnlyDictionary<string, string> speakerLabels)
    {
        var sb = new StringBuilder();
        sb.AppendLine("WEBVTT");
        sb.AppendLine();
        foreach (var segment in segments.OrderBy(s => s.StartMs))
        {
            sb.AppendLine($"{FormatVttTimestamp(segment.StartMs)} --> {FormatVttTimestamp(segment.EndMs)}");
            sb.AppendLine(WithSpeakerPrefix(segment, speakerLabels));
            sb.AppendLine();
        }
        return sb.ToString();
    }

    public static string ToTxt(IReadOnlyList<TranscriptSegment> segments, IReadOnlyDictionary<string, string> speakerLabels)
    {
        var sb = new StringBuilder();
        foreach (var segment in segments.OrderBy(s => s.StartMs))
            sb.AppendLine(WithSpeakerPrefix(segment, speakerLabels));
        return sb.ToString();
    }

    private static string WithSpeakerPrefix(TranscriptSegment segment, IReadOnlyDictionary<string, string> speakerLabels)
    {
        var label = segment.SpeakerId is not null && speakerLabels.TryGetValue(segment.SpeakerId, out var l) ? l : "Unknown";
        return $"{label}: {segment.Text}";
    }

    private static string FormatSrtTimestamp(long ms) =>
        TimeSpan.FromMilliseconds(ms).ToString(@"hh\:mm\:ss\,fff");

    private static string FormatVttTimestamp(long ms) =>
        TimeSpan.FromMilliseconds(ms).ToString(@"hh\:mm\:ss\.fff");
}
