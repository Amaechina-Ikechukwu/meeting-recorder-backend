using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using MeetingRecorder.Application.Abstractions;
using MeetingRecorder.Domain.Entities;

namespace MeetingRecorder.Infrastructure.Export;

public class OpenXmlDocxExporter : IDocxExporter
{
    public Task<Stream> ExportAsync(
        string meetingTitle,
        IReadOnlyList<TranscriptSegment> segments,
        IReadOnlyDictionary<string, string> speakerLabels,
        CancellationToken ct = default)
    {
        var stream = new MemoryStream();

        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, autoSave: false))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());

            body.AppendChild(new Paragraph(new Run(new RunProperties(new Bold()), new Text(meetingTitle))));

            foreach (var segment in segments.OrderBy(s => s.StartMs))
            {
                var label = segment.SpeakerId is not null && speakerLabels.TryGetValue(segment.SpeakerId, out var l)
                    ? l
                    : "Unknown";
                var timestamp = TimeSpan.FromMilliseconds(segment.StartMs).ToString(@"hh\:mm\:ss");

                var paragraph = new Paragraph(
                    new Run(new RunProperties(new Bold()), new Text($"[{timestamp}] {label}: ") { Space = SpaceProcessingModeValues.Preserve }),
                    new Run(new Text(segment.Text) { Space = SpaceProcessingModeValues.Preserve }));

                body.AppendChild(paragraph);
            }

            mainPart.Document.Save();
        }

        stream.Position = 0;
        return Task.FromResult<Stream>(stream);
    }
}
