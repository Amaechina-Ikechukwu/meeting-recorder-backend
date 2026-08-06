using System.Net.Http.Headers;
using System.Net.Http.Json;
using MeetingRecorder.Application.Abstractions;
using MeetingRecorder.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace MeetingRecorder.Infrastructure.SpeechToText;

/// <summary>Thin wrapper over Deepgram's prerecorded /v1/listen endpoint. Returns
/// word-level results with per-word speaker labels (diarize=true), which both the
/// transcription and diarization engines derive their output from.</summary>
internal class DeepgramClient(HttpClient httpClient, IRecordingStorage storage, IOptions<DeepgramOptions> options)
{
    private readonly DeepgramOptions _options = options.Value;

    public async Task<List<DeepgramWord>> TranscribeWordsAsync(string storageKey, string contentType, CancellationToken ct = default)
    {
        await using var audio = await storage.OpenReadAsync(storageKey, ct);
        using var content = new StreamContent(audio);
        content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"v1/listen?model={_options.Model}&diarize=true&punctuate=true&smart_format=true")
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Token", _options.ApiKey);

        var response = await httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var parsed = await response.Content.ReadFromJsonAsync<DeepgramResponse>(cancellationToken: ct);
        return parsed?.Results?.Channels.FirstOrDefault()?.Alternatives.FirstOrDefault()?.Words ?? [];
    }
}
