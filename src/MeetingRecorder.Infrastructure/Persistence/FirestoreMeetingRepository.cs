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

    public Task UpdateAsync(Meeting meeting, CancellationToken ct = default) =>
        Collection.Document(meeting.Id).SetAsync(meeting.ToDocument(), SetOptions.MergeAll, ct);
}
