namespace MeetingRecorder.Infrastructure.Options;

public class DeepgramOptions
{
    public const string SectionName = "Deepgram";

    public required string ApiKey { get; set; }
    public string BaseUrl { get; set; } = "https://api.deepgram.com";
    public string Model { get; set; } = "nova-2";
}
