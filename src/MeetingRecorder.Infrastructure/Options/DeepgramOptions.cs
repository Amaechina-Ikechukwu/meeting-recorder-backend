namespace MeetingRecorder.Infrastructure.Options;

public class DeepgramOptions
{
    public const string SectionName = "Deepgram";

    public required string ApiKey { get; set; }
    public string BaseUrl { get; set; } = "https://api.deepgram.com";
    public string Model { get; set; } = "nova-2";

    /// <summary>
    /// The batch diarization model. "latest" selects Deepgram's current diarizer instead of
    /// the legacy v1 model selected by the deprecated <c>diarize=true</c> parameter. Speaker
    /// labels come back on the words of the transcription response, so this costs no extra
    /// request.
    /// </summary>
    public string DiarizationModel { get; set; } = "latest";

    /// <summary>
    /// Hand Deepgram a signed URL to the stored object and let it read from GCS directly,
    /// instead of pulling the audio into this process and posting the bytes back out.
    /// Turn off only if the bucket is unreachable from Deepgram's network; the client falls
    /// back to streaming on its own if signing fails.
    /// </summary>
    public bool UseSignedSourceUrl { get; set; } = true;

    /// <summary>How long the signed URL handed to Deepgram stays valid.</summary>
    public int SourceUrlValidMinutes { get; set; } = 30;

    /// <summary>
    /// Ceiling on a single transcription call. HttpClient's 100s default cut long
    /// recordings off mid-transcription and surfaced as a failed meeting.
    /// </summary>
    public int RequestTimeoutMinutes { get; set; } = 15;
}
