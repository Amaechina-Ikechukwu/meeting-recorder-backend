using Google.Cloud.Firestore;

namespace MeetingRecorder.Infrastructure.Persistence.Documents;

[FirestoreData]
public class SpeakerDocument
{
    [FirestoreProperty("meetingId")]
    public string MeetingId { get; set; } = string.Empty;

    [FirestoreProperty("label")]
    public string Label { get; set; } = string.Empty;

    [FirestoreProperty("totalSpeakingMs")]
    public double TotalSpeakingMs { get; set; }
}
