namespace MeetingRecorder.Infrastructure.Options;

public class ZeptoMailOptions
{
    public const string SectionName = "ZeptoMail";

    public required string ApiKey { get; set; }
    public string BaseUrl { get; set; } = "https://api.zeptomail.com";
    public string FromAddress { get; set; } = "noreply@example.com";
    public string FromName { get; set; } = "Meeting Recorder";
}
