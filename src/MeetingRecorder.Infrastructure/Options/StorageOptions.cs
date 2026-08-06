namespace MeetingRecorder.Infrastructure.Options;

public class StorageOptions
{
    public const string SectionName = "Storage";

    public required string BucketName { get; set; }
    public int UploadUrlValidMinutes { get; set; } = 30;
    public int RawAudioRetentionDays { get; set; } = 90;
}
