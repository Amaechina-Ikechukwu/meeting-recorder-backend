using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using MeetingRecorder.Application.Abstractions;
using MeetingRecorder.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace MeetingRecorder.Infrastructure.Storage;

/// <summary>Stores audio and derived artifacts in a GCS bucket (same project as Firebase Storage).</summary>
public class GcsRecordingStorage(StorageClient client, GoogleCredential credential, IOptions<StorageOptions> options)
    : IRecordingStorage
{
    private readonly StorageOptions _options = options.Value;
    private readonly UrlSigner _urlSigner = UrlSigner.FromCredential(credential);

    public async Task<SignedUploadUrl> CreateUploadUrlAsync(string storageKey, string contentType, CancellationToken ct = default)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.UploadUrlValidMinutes);
        var url = await _urlSigner.SignAsync(
            _options.BucketName,
            storageKey,
            TimeSpan.FromMinutes(_options.UploadUrlValidMinutes),
            HttpMethod.Put,
            cancellationToken: ct);

        return new SignedUploadUrl(new Uri(url), storageKey, expiresAt);
    }

    public async Task<Uri> CreatePlaybackUrlAsync(string storageKey, TimeSpan validFor, CancellationToken ct = default)
    {
        var url = await _urlSigner.SignAsync(_options.BucketName, storageKey, validFor, HttpMethod.Get, cancellationToken: ct);
        return new Uri(url);
    }

    public async Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct = default)
    {
        var stream = new MemoryStream();
        await client.DownloadObjectAsync(_options.BucketName, storageKey, stream, cancellationToken: ct);
        stream.Position = 0;
        return stream;
    }

    public async Task WriteAsync(string storageKey, Stream content, string contentType, CancellationToken ct = default) =>
        await client.UploadObjectAsync(_options.BucketName, storageKey, contentType, content, cancellationToken: ct);

    public async Task<bool> ExistsAsync(string storageKey, CancellationToken ct = default)
    {
        try
        {
            await client.GetObjectAsync(_options.BucketName, storageKey, cancellationToken: ct);
            return true;
        }
        catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task DeleteAsync(string storageKey, CancellationToken ct = default) =>
        await client.DeleteObjectAsync(_options.BucketName, storageKey, cancellationToken: ct);
}
