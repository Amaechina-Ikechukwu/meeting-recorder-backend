using System.Text.Json.Serialization;

namespace MeetingRecorder.Infrastructure.SpeechToText;

internal class DeepgramResponse
{
    [JsonPropertyName("results")]
    public DeepgramResults? Results { get; set; }
}

internal class DeepgramResults
{
    [JsonPropertyName("channels")]
    public List<DeepgramChannel> Channels { get; set; } = [];
}

internal class DeepgramChannel
{
    [JsonPropertyName("alternatives")]
    public List<DeepgramAlternative> Alternatives { get; set; } = [];
}

internal class DeepgramAlternative
{
    [JsonPropertyName("words")]
    public List<DeepgramWord> Words { get; set; } = [];
}

internal class DeepgramWord
{
    [JsonPropertyName("word")]
    public string Word { get; set; } = string.Empty;

    [JsonPropertyName("start")]
    public double Start { get; set; }

    [JsonPropertyName("end")]
    public double End { get; set; }

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    [JsonPropertyName("speaker")]
    public int? Speaker { get; set; }

    [JsonPropertyName("punctuated_word")]
    public string? PunctuatedWord { get; set; }
}
