using System.Net.Http.Headers;
using System.Net.Http.Json;
using MeetingRecorder.Application.Abstractions;
using MeetingRecorder.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MeetingRecorder.Infrastructure.SpeechToText;

/// <summary>Thin wrapper over Deepgram's prerecorded /v1/listen endpoint. Returns
/// word-level results with per-word speaker labels, which both the
/// transcription and diarization engines derive their output from.</summary>
/// <remarks>
/// The audio is handed over as a signed URL rather than as bytes. Streaming it meant
/// downloading the whole recording from GCS into this process and uploading the same bytes
/// again to Deepgram — two full transfers of the meeting on the critical path, plus a
/// MemoryStream the size of the audio held for the duration of the call. Deepgram fetching
/// the object itself removes both. Streaming remains as a fallback for when a URL cannot be
/// signed or Deepgram cannot reach the bucket.
/// </remarks>
internal class DeepgramClient(
    HttpClient httpClient,
    IRecordingStorage storage,
    IOptions<DeepgramOptions> options,
    ILogger<DeepgramClient> logger)
{
    private readonly DeepgramOptions _options = options.Value;

    public async Task<List<DeepgramWord>> TranscribeWordsAsync(string storageKey, string contentType, CancellationToken ct = default)
    {
        if (_options.UseSignedSourceUrl)
        {
            try
            {
                var source = await storage.CreatePlaybackUrlAsync(
                    storageKey, TimeSpan.FromMinutes(_options.SourceUrlValidMinutes), ct);

                using var request = CreateRequest();
                request.Content = JsonContent.Create(new { url = source.AbsoluteUri });
                return await SendAsync(request, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(
                    ex,
                    "Deepgram could not transcribe {StorageKey} from a signed URL; streaming the audio instead.",
                    storageKey);
            }
        }

        await using var audio = await storage.OpenReadAsync(storageKey, ct);
        using var streamed = CreateRequest();
        var content = new StreamContent(audio);
        content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        streamed.Content = content;
        return await SendAsync(streamed, ct);
    }

    private HttpRequestMessage CreateRequest()
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"v1/listen?model={Uri.EscapeDataString(_options.Model)}&diarize_model={Uri.EscapeDataString(_options.DiarizationModel)}&punctuate=true&smart_format=true");
        request.Headers.Authorization = new AuthenticationHeaderValue("Token", _options.ApiKey);
        return request;
    }

    private async Task<List<DeepgramWord>> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var response = await httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var parsed = await response.Content.ReadFromJsonAsync<DeepgramResponse>(cancellationToken: ct);
        return parsed?.Results?.Channels.FirstOrDefault()?.Alternatives.FirstOrDefault()?.Words ?? [];
    }
}
