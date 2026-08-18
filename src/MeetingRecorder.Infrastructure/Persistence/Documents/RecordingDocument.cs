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

    [FirestoreProperty("createdAt")]
    public Timestamp CreatedAt { get; set; }
}
