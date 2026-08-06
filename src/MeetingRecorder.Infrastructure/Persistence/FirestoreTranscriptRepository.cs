using Google.Cloud.Firestore;
using MeetingRecorder.Application.Abstractions;
using MeetingRecorder.Domain.Entities;
using MeetingRecorder.Infrastructure.Persistence.Documents;

namespace MeetingRecorder.Infrastructure.Persistence;

public class FirestoreTranscriptRepository(FirestoreDb db) : ITranscriptRepository
{
    private DocumentReference MeetingDoc(string meetingId) =>
        db.Collection(FirestoreCollectionNames.Meetings).Document(meetingId);

    private CollectionReference SegmentsOf(string meetingId) =>
        MeetingDoc(meetingId).Collection(FirestoreCollectionNames.TranscriptSegments);

    public async Task<Transcript?> GetByMeetingIdAsync(string meetingId, CancellationToken ct = default)
    {
        var meetingSnapshot = await MeetingDoc(meetingId).GetSnapshotAsync(ct);
        if (!meetingSnapshot.Exists)
            return null;

        var meetingDoc = meetingSnapshot.ConvertTo<MeetingDocument>();
        if (meetingDoc.TranscriptId is null)
            return null;

        var segmentsSnapshot = await SegmentsOf(meetingId).GetSnapshotAsync(ct);
        var segments = segmentsSnapshot.Documents
            .Select(d => d.ConvertTo<TranscriptSegmentDocument>().ToDomain(d.Id))
            .ToList();

        return new Transcript
        {
            Id = meetingDoc.TranscriptId,
            MeetingId = meetingId,
            RecordingId = meetingDoc.TranscriptRecordingId ?? string.Empty,
            CreatedAt = meetingDoc.TranscriptCreatedAt?.ToDateTimeOffset() ?? DateTimeOffset.UtcNow,
            Segments = segments
        };
    }

    public async Task SaveAsync(Transcript transcript, CancellationToken ct = default)
    {
        await MeetingDoc(transcript.MeetingId).SetAsync(new MeetingDocument
        {
            TranscriptId = transcript.Id,
            TranscriptRecordingId = transcript.RecordingId,
            TranscriptCreatedAt = Timestamp.FromDateTimeOffset(transcript.CreatedAt)
        }, SetOptions.MergeAll, ct);

        var existingIds = (await SegmentsOf(transcript.MeetingId).GetSnapshotAsync(ct))
            .Documents.Select(d => d.Id).ToHashSet();
        var newIds = transcript.Segments.Select(s => s.Id).ToHashSet();

        var batch = db.StartBatch();
        foreach (var segment in transcript.Segments)
            batch.Set(SegmentsOf(transcript.MeetingId).Document(segment.Id), segment.ToDocument());
        foreach (var staleId in existingIds.Except(newIds))
            batch.Delete(SegmentsOf(transcript.MeetingId).Document(staleId));

        await batch.CommitAsync(ct);
    }

    public async Task<IReadOnlyList<TranscriptSegment>> SearchAsync(string meetingId, string query, CancellationToken ct = default)
    {
        var snapshot = await SegmentsOf(meetingId).GetSnapshotAsync(ct);
        return [.. snapshot.Documents
            .Select(d => d.ConvertTo<TranscriptSegmentDocument>().ToDomain(d.Id))
            .Where(s => s.Text.Contains(query, StringComparison.OrdinalIgnoreCase))];
    }
}
