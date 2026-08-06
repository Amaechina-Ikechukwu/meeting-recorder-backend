namespace MeetingRecorder.Application.Dtos;

public record TranscriptSegmentDto(
    string Id,
    string? SpeakerId,
    string? SpeakerLabel,
    string Text,
    long StartMs,
    long EndMs,
    double Confidence);

public record TranscriptDto(string Id, string MeetingId, DateTimeOffset CreatedAt, List<TranscriptSegmentDto> Segments);

public record RenameSpeakerRequest(string Label);

public record SpeakerDto(string Id, string MeetingId, string Label, double TotalSpeakingMs);

public enum ExportFormat
{
    Srt,
    Vtt,
    Docx,
    Txt
}
