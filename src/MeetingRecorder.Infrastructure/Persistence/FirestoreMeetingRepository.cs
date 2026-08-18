using Google.Cloud.Firestore;
using MeetingRecorder.Application.Abstractions;
using MeetingRecorder.Domain.Entities;
using MeetingRecorder.Infrastructure.Persistence.Documents;

namespace MeetingRecorder.Infrastructure.Persistence;

public class FirestoreMeetingRepository(FirestoreDb db) : IMeetingRepository
{
    private CollectionReference Collection => db.Collection(FirestoreCollectionNames.Meetings);

    public async Task<Meeting?> GetByIdAsync(string meetingId, CancellationToken ct = default)
    {
        var snapshot = await Collection.Document(meetingId).GetSnapshotAsync(ct);
        return snapshot.Exists ? snapshot.ConvertTo<MeetingDocument>().ToDomain(snapshot.Id) : null;
    }

    public async Task<IReadOnlyList<Meeting>> GetByOwnerAsync(string ownerId, CancellationToken ct = default)
    {
        var snapshot = await Collection.WhereEqualTo("ownerId", ownerId).GetSnapshotAsync(ct);
        return [.. snapshot.Documents.Select(d => d.ConvertTo<MeetingDocument>().ToDomain(d.Id))];
    }

    public Task CreateAsync(Meeting meeting, CancellationToken ct = default) =>
        Collection.Document(meeting.Id).SetAsync(meeting.ToDocument(), cancellationToken: ct);

    /// <summary>
    /// The meeting's own fields. The transcript pointers live on the same document but
    /// belong to FirestoreTranscriptRepository, and ToDocument leaves them null — with
    /// MergeAll that null would be written, silently detaching the transcript from a
    /// meeting whose status had merely changed.
    /// </summary>
    private static readonly SetOptions MeetingFields = SetOptions.MergeFields(
        "ownerId", "title", "status", "failureReason", "failureMessage", "createdAt", "participantHints");

    public Task UpdateAsync(Meeting meeting, CancellationToken ct = default) =>
        Collection.Document(meeting.Id).SetAsync(meeting.ToDocument(), MeetingFields, ct);
}
