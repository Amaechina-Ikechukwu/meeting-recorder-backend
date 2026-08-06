using Google.Cloud.Firestore;

namespace MeetingRecorder.Infrastructure.Persistence.Documents;

[FirestoreData]
public class RecordingDocument
{
    [FirestoreProperty("meetingId")]
    public string MeetingId { get; set; } = string.Empty;

    [FirestoreProperty("storageKey")]
    public string StorageKey { get; set; } = string.Empty;

    [FirestoreProperty("contentType")]
    public string ContentType { get; set; } = string.Empty;

    [FirestoreProperty("durationMs")]
    public long DurationMs { get; set; }

    [FirestoreProperty("status")]
    public string Status { get; set; } = string.Empty;

    [FirestoreProperty("transcriptionReady")]
    public bool TranscriptionReady { get; set; }

    [FirestoreProperty("diarizationReady")]
    public bool DiarizationReady { get; set; }

    [FirestoreProperty("speakerTurns")]
    public List<SpeakerTurnDocument> SpeakerTurns { get; set; } = [];

    [FirestoreProperty("createdAt")]
    public Timestamp CreatedAt { get; set; }
}

[FirestoreData]
public class SpeakerTurnDocument
{
    [FirestoreProperty("startMs")]
    public long StartMs { get; set; }

    [FirestoreProperty("endMs")]
    public long EndMs { get; set; }

    [FirestoreProperty("speakerLabel")]
    public string SpeakerLabel { get; set; } = string.Empty;
}
