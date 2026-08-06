using Google.Cloud.Firestore;

namespace MeetingRecorder.Infrastructure.Persistence.Documents;

[FirestoreData]
public class MeetingDocument
{
    [FirestoreProperty("ownerId")]
    public string OwnerId { get; set; } = string.Empty;

    [FirestoreProperty("title")]
    public string Title { get; set; } = string.Empty;

    [FirestoreProperty("status")]
    public string Status { get; set; } = string.Empty;

    [FirestoreProperty("failureReason")]
    public string FailureReason { get; set; } = string.Empty;

    [FirestoreProperty("failureMessage")]
    public string? FailureMessage { get; set; }

    [FirestoreProperty("createdAt")]
    public Timestamp CreatedAt { get; set; }

    [FirestoreProperty("participantHints")]
    public List<string> ParticipantHints { get; set; } = [];

    // Transcript is 1:1 with the meeting; segments live in the transcriptSegments subcollection.
    [FirestoreProperty("transcriptId")]
    public string? TranscriptId { get; set; }

    [FirestoreProperty("transcriptRecordingId")]
    public string? TranscriptRecordingId { get; set; }

    [FirestoreProperty("transcriptCreatedAt")]
    public Timestamp? TranscriptCreatedAt { get; set; }
}
