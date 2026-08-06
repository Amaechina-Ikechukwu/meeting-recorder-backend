using Google.Cloud.Firestore;

namespace MeetingRecorder.Infrastructure.Persistence.Documents;

[FirestoreData]
public class TranscriptSegmentDocument
{
    [FirestoreProperty("transcriptId")]
    public string TranscriptId { get; set; } = string.Empty;

    [FirestoreProperty("speakerId")]
    public string? SpeakerId { get; set; }

    [FirestoreProperty("text")]
    public string Text { get; set; } = string.Empty;

    [FirestoreProperty("startMs")]
    public long StartMs { get; set; }

    [FirestoreProperty("endMs")]
    public long EndMs { get; set; }

    [FirestoreProperty("confidence")]
    public double Confidence { get; set; }
}
