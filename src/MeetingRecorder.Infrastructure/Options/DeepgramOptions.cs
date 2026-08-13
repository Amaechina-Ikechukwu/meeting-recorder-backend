namespace MeetingRecorder.Infrastructure.Options;

public class DeepgramOptions
{
    public const string SectionName = "Deepgram";

    public required string ApiKey { get; set; }
    public string BaseUrl { get; set; } = "https://api.deepgram.com";
    public string Model { get; set; } = "nova-2";

    /// <summary>
    /// The batch diarization model to use. "latest" selects Deepgram's current
    /// diarizer instead of the legacy v1 model selected by the deprecated
    /// <c>diarize=true</c> query parameter.
    /// </summary>
    public string DiarizationModel { get; set; } = "latest";
}
