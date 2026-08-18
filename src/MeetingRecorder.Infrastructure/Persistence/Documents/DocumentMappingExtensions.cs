using Google.Cloud.Firestore;
using MeetingRecorder.Domain.Entities;
using MeetingRecorder.Domain.Enums;

namespace MeetingRecorder.Infrastructure.Persistence.Documents;

public static class DocumentMappingExtensions
{
    /// <summary>
    /// Reads a stored enum without throwing on a value this code cannot make sense of.
    /// A blank or unrecognised status otherwise makes the whole document unreadable —
    /// Enum.Parse on "" throws — which turns one bad field into a meeting the owner can
    /// neither open nor retry. Reporting it as failed at least leaves the retry path open.
    /// </summary>
    private static TEnum ParseOr<TEnum>(string? stored, TEnum fallback) where TEnum : struct =>
        Enum.TryParse<TEnum>(stored, out var parsed) ? parsed : fallback;

    public static MeetingDocument ToDocument(this Meeting meeting) => new()
    {
        OwnerId = meeting.OwnerId,
        Title = meeting.Title,
        Status = meeting.Status.ToString(),
        FailureReason = meeting.FailureReason.ToString(),
        FailureMessage = meeting.FailureMessage,
        CreatedAt = Timestamp.FromDateTimeOffset(meeting.CreatedAt),
        ParticipantHints = meeting.ParticipantHints
    };

    public static Meeting ToDomain(this MeetingDocument doc, string id) => new()
    {
        Id = id,
        OwnerId = doc.OwnerId,
        Title = doc.Title,
        Status = ParseOr(doc.Status, MeetingStatus.Failed),
        FailureReason = ParseOr(doc.FailureReason, FailureReason.None),
        FailureMessage = doc.FailureMessage,
        CreatedAt = doc.CreatedAt.ToDateTimeOffset(),
        ParticipantHints = doc.ParticipantHints
    };

    public static RecordingDocument ToDocument(this Recording recording) => new()
    {
        MeetingId = recording.MeetingId,
        StorageKey = recording.StorageKey,
        ContentType = recording.ContentType,
        DurationMs = recording.DurationMs,
        Status = recording.Status.ToString(),
        TranscriptionReady = recording.TranscriptionReady,
        CreatedAt = Timestamp.FromDateTimeOffset(recording.CreatedAt)
    };

    public static Recording ToDomain(this RecordingDocument doc, string id) => new()
    {
        Id = id,
        MeetingId = doc.MeetingId,
        StorageKey = doc.StorageKey,
        ContentType = doc.ContentType,
        DurationMs = doc.DurationMs,
        Status = ParseOr(doc.Status, RecordingStatus.Failed),
        TranscriptionReady = doc.TranscriptionReady,
        CreatedAt = doc.CreatedAt.ToDateTimeOffset()
    };

    public static TranscriptSegmentDocument ToDocument(this TranscriptSegment segment) => new()
    {
        TranscriptId = segment.TranscriptId,
        SpeakerId = segment.SpeakerId,
        Text = segment.Text,
        StartMs = segment.StartMs,
        EndMs = segment.EndMs,
        Confidence = segment.Confidence
    };

    public static TranscriptSegment ToDomain(this TranscriptSegmentDocument doc, string id) => new()
    {
        Id = id,
        TranscriptId = doc.TranscriptId,
        SpeakerId = doc.SpeakerId,
        Text = doc.Text,
        StartMs = doc.StartMs,
        EndMs = doc.EndMs,
        Confidence = doc.Confidence
    };

    public static SpeakerDocument ToDocument(this Speaker speaker) => new()
    {
        MeetingId = speaker.MeetingId,
        Label = speaker.Label,
        TotalSpeakingMs = speaker.TotalSpeakingMs
    };

    public static Speaker ToDomain(this SpeakerDocument doc, string id) => new()
    {
        Id = id,
        MeetingId = doc.MeetingId,
        Label = doc.Label,
        TotalSpeakingMs = doc.TotalSpeakingMs
    };
}
