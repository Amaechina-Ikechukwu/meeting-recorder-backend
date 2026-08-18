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
    private readonly string _bucket = NormalizeBucketName(options.Value.BucketName);

    /// <summary>
    /// Accepts a bare bucket name or a "gs://bucket" URI. The GCS client rejects the URI
    /// form with "Invalid bucket name", and pasting the gs:// form into configuration is an
    /// easy mistake, so strip the scheme and any object path here rather than 500 per request.
    /// </summary>
    internal static string NormalizeBucketName(string? configured)
    {
        var value = (configured ?? string.Empty).Trim();
        const string scheme = "gs://";
        if (value.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
            value = value[scheme.Length..];

        value = value.Trim('/');
        var slash = value.IndexOf('/');
        if (slash >= 0)
            value = value[..slash];

        if (value.Length == 0)
            throw new InvalidOperationException(
                "Storage:BucketName is not configured. Set it to the bucket name (e.g. \"my-bucket\"), not a gs:// URI.");

        return value;
    }

    public async Task<SignedUploadUrl> CreateUploadUrlAsync(string storageKey, string contentType, CancellationToken ct = default)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.UploadUrlValidMinutes);
        var url = await _urlSigner.SignAsync(
            _bucket,
            storageKey,
            TimeSpan.FromMinutes(_options.UploadUrlValidMinutes),
            HttpMethod.Put,
            cancellationToken: ct);

        return new SignedUploadUrl(new Uri(url), storageKey, expiresAt);
    }

    public async Task<Uri> CreatePlaybackUrlAsync(string storageKey, TimeSpan validFor, CancellationToken ct = default)
    {
        var url = await _urlSigner.SignAsync(_bucket, storageKey, validFor, HttpMethod.Get, cancellationToken: ct);
        return new Uri(url);
    }

    public async Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct = default)
    {
        var stream = new MemoryStream();
        await client.DownloadObjectAsync(_bucket, storageKey, stream, cancellationToken: ct);
        stream.Position = 0;
        return stream;
    }

    public async Task WriteAsync(string storageKey, Stream content, string contentType, CancellationToken ct = default) =>
        await client.UploadObjectAsync(_bucket, storageKey, contentType, content, cancellationToken: ct);

    public async Task<bool> ExistsAsync(string storageKey, CancellationToken ct = default)
    {
        try
        {
            await client.GetObjectAsync(_bucket, storageKey, cancellationToken: ct);
            return true;
        }
        catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task DeleteAsync(string storageKey, CancellationToken ct = default) =>
        await client.DeleteObjectAsync(_bucket, storageKey, cancellationToken: ct);
}
