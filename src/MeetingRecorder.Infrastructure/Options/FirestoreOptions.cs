namespace MeetingRecorder.Infrastructure.Options;

public class FirestoreOptions
{
    public const string SectionName = "Firestore";

    public required string ProjectId { get; set; }
}
