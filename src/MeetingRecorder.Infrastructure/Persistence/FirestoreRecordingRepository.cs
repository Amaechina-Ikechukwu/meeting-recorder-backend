using Google.Cloud.Firestore;
using MeetingRecorder.Application.Abstractions;
using MeetingRecorder.Domain.Entities;
using MeetingRecorder.Domain.Enums;
using MeetingRecorder.Domain.ValueObjects;
using MeetingRecorder.Infrastructure.Persistence.Documents;

namespace MeetingRecorder.Infrastructure.Persistence;

public class FirestoreRecordingRepository(FirestoreDb db) : IRecordingRepository
{
    private CollectionReference RecordingsOf(string meetingId) =>
        db.Collection(FirestoreCollectionNames.Meetings).Document(meetingId).Collection(FirestoreCollectionNames.Recordings);

    public async Task<Recording?> GetByIdAsync(string meetingId, string recordingId, CancellationToken ct = default)
    {
        var snapshot = await RecordingsOf(meetingId).Document(recordingId).GetSnapshotAsync(ct);
        return snapshot.Exists ? snapshot.ConvertTo<RecordingDocument>().ToDomain(snapshot.Id) : null;
    }

    public Task CreateAsync(Recording recording, CancellationToken ct = default) =>
        RecordingsOf(recording.MeetingId).Document(recording.Id).SetAsync(recording.ToDocument(), cancellationToken: ct);

    public Task UpdateAsync(Recording recording, CancellationToken ct = default) =>
        RecordingsOf(recording.MeetingId).Document(recording.Id).SetAsync(recording.ToDocument(), SetOptions.MergeAll, ct);

    public Task MarkTranscriptionReadyAsync(string meetingId, string recordingId, CancellationToken ct = default) =>
        RecordingsOf(meetingId).Document(recordingId).UpdateAsync("transcriptionReady", true, cancellationToken: ct);

    public Task SaveDiarizationResultAsync(string meetingId, string recordingId, IReadOnlyList<SpeakerTurn> turns, CancellationToken ct = default) =>
        RecordingsOf(meetingId).Document(recordingId).UpdateAsync(new Dictionary<string, object>
        {
            ["speakerTurns"] = turns.Select(t => new SpeakerTurnDocument
            {
                StartMs = t.StartMs,
                EndMs = t.EndMs,
                SpeakerLabel = t.SpeakerLabel
            }).ToList(),
            ["diarizationReady"] = true
        }, cancellationToken: ct);

    public Task UpdateStatusAsync(string meetingId, string recordingId, RecordingStatus status, CancellationToken ct = default) =>
        RecordingsOf(meetingId).Document(recordingId).UpdateAsync("status", status.ToString(), cancellationToken: ct);
}
