using System.Text;
using MeetingRecorder.Application.Abstractions;
using MeetingRecorder.Application.Dtos;
using MeetingRecorder.Application.Exceptions;
using MeetingRecorder.Application.Services;
using MeetingRecorder.Domain.Entities;

namespace MeetingRecorder.Application.UseCases.Transcripts;

public record ExportResult(Stream Content, string ContentType, string FileName);

/// <summary>Renders the merged transcript into the requested export format, caching the
/// result in blob storage so repeat requests don't re-render.</summary>
public class ExportTranscript(
    IMeetingRepository meetings,
    ITranscriptRepository transcripts,
    ISpeakerRepository speakers,
    IRecordingStorage storage,
    IDocxExporter docxExporter)
{
    public async Task<ExportResult> ExecuteAsync(
        string requesterId, string meetingId, ExportFormat format, CancellationToken ct = default)
    {
        var meeting = await meetings.GetByIdAsync(meetingId, ct)
            ?? throw new NotFoundException(nameof(Meeting), meetingId);

        if (meeting.OwnerId != requesterId)
            throw new ForbiddenAccessException();

        var extension = format.ToString().ToLowerInvariant();
        var cacheKey = $"meetings/{meetingId}/export/transcript.{extension}";

        if (await storage.ExistsAsync(cacheKey, ct))
        {
            var cached = await storage.OpenReadAsync(cacheKey, ct);
            return new ExportResult(cached, ContentTypeFor(format), $"{meeting.Title}.{extension}");
        }

        var transcript = await transcripts.GetByMeetingIdAsync(meetingId, ct)
            ?? throw new NotFoundException(nameof(Transcript), meetingId);
        var speakerList = await speakers.GetByMeetingIdAsync(meetingId, ct);
        var labels = speakerList.ToDictionary(s => s.Id, s => s.Label);
        var segments = transcript.Segments.OrderBy(s => s.StartMs).ToList();

        Stream content = format switch
        {
            ExportFormat.Srt => ToStream(TranscriptFormatter.ToSrt(segments, labels)),
            ExportFormat.Vtt => ToStream(TranscriptFormatter.ToVtt(segments, labels)),
            ExportFormat.Txt => ToStream(TranscriptFormatter.ToTxt(segments, labels)),
            ExportFormat.Docx => await docxExporter.ExportAsync(meeting.Title, segments, labels, ct),
            _ => throw new ValidationFailedException(["Unsupported export format."])
        };

        content.Position = 0;
        await storage.WriteAsync(cacheKey, content, ContentTypeFor(format), ct);
        content.Position = 0;

        return new ExportResult(content, ContentTypeFor(format), $"{meeting.Title}.{extension}");
    }

    private static Stream ToStream(string text) => new MemoryStream(Encoding.UTF8.GetBytes(text));

    private static string ContentTypeFor(ExportFormat format) => format switch
    {
        ExportFormat.Srt => "application/x-subrip",
        ExportFormat.Vtt => "text/vtt",
        ExportFormat.Txt => "text/plain",
        ExportFormat.Docx => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        _ => "application/octet-stream"
    };
}
