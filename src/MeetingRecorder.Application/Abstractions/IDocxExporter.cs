using MeetingRecorder.Domain.Entities;

namespace MeetingRecorder.Application.Abstractions;

public interface IDocxExporter
{
    Task<Stream> ExportAsync(
        string meetingTitle,
        IReadOnlyList<TranscriptSegment> segments,
        IReadOnlyDictionary<string, string> speakerLabels,
        CancellationToken ct = default);
}
