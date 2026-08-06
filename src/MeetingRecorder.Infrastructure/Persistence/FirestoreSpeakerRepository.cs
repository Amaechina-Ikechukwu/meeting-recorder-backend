using Google.Cloud.Firestore;
using MeetingRecorder.Application.Abstractions;
using MeetingRecorder.Domain.Entities;
using MeetingRecorder.Infrastructure.Persistence.Documents;

namespace MeetingRecorder.Infrastructure.Persistence;

public class FirestoreSpeakerRepository(FirestoreDb db) : ISpeakerRepository
{
    private CollectionReference SpeakersOf(string meetingId) =>
        db.Collection(FirestoreCollectionNames.Meetings).Document(meetingId).Collection(FirestoreCollectionNames.Speakers);

    public async Task<IReadOnlyList<Speaker>> GetByMeetingIdAsync(string meetingId, CancellationToken ct = default)
    {
        var snapshot = await SpeakersOf(meetingId).GetSnapshotAsync(ct);
        return [.. snapshot.Documents.Select(d => d.ConvertTo<SpeakerDocument>().ToDomain(d.Id))];
    }

    public async Task<Speaker?> GetByIdAsync(string meetingId, string speakerId, CancellationToken ct = default)
    {
        var snapshot = await SpeakersOf(meetingId).Document(speakerId).GetSnapshotAsync(ct);
        return snapshot.Exists ? snapshot.ConvertTo<SpeakerDocument>().ToDomain(snapshot.Id) : null;
    }

    public Task UpsertAsync(Speaker speaker, CancellationToken ct = default) =>
        SpeakersOf(speaker.MeetingId).Document(speaker.Id).SetAsync(speaker.ToDocument(), cancellationToken: ct);
}
