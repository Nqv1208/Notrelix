using System.Net;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;

namespace Notrelix.Infrastructure.Storage.Providers;

/// <summary>
/// S3-compatible object storage (AWS S3, MinIO, Cloudflare R2). The bucket is private: every object is reached through this provider or
/// through a pre-signed URL that the owning use case asked for after its access check. Pre-signing is local computation with the
/// configured credentials, so it makes no network call. Object keys are the server-generated attachment keys, validated before use.
/// A URL, a secret and a request signature are never logged.
/// </summary>
public sealed class S3StorageProvider : IStorageService, IPresignedObjectStorage, IDisposable
{
    private const string LogicalNotFound = "NoSuchKey";

    private readonly AmazonS3Client _client;
    private readonly string _bucket;
    private readonly ILogger<S3StorageProvider> _logger;

    public S3StorageProvider(IOptions<StorageOptions> options, ILogger<S3StorageProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _logger = logger;
        var settings = options.Value.S3;
        _bucket = settings.Bucket;

        var config = new AmazonS3Config
        {
            AuthenticationRegion = settings.Region,
            ForcePathStyle = settings.ForcePathStyle,
        };
        if (!string.IsNullOrWhiteSpace(settings.ServiceUrl))
        {
            config.ServiceURL = settings.ServiceUrl;
        }

        _client = string.IsNullOrWhiteSpace(settings.AccessKeyId)
            ? new AmazonS3Client(config)
            : new AmazonS3Client(new BasicAWSCredentials(settings.AccessKeyId, settings.SecretAccessKey), config);
    }

    public bool SupportsPresignedUrls => true;

    public async Task WriteAsync(string key, Stream content, string contentType, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        RequireKey(key);
        if (await HeadAsync(key, cancellationToken) is not null)
        {
            throw new IOException("An object already exists at the requested key.");
        }

        try
        {
            using var transfer = new TransferUtility(_client);
            await transfer.UploadAsync(new TransferUtilityUploadRequest
            {
                BucketName = _bucket,
                Key = key,
                InputStream = content,
                AutoCloseStream = false,
                ContentType = contentType,
            }, cancellationToken);
        }
        catch
        {
            // Nothing may remain at the key after a failed or aborted write (the same guarantee as the filesystem provider).
            await TryDeleteAsync(key);
            throw;
        }
    }

    public async Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken)
    {
        RequireKey(key);
        try
        {
            var response = await _client.GetObjectAsync(new GetObjectRequest { BucketName = _bucket, Key = key }, cancellationToken);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception ex) when (IsNotFound(ex))
        {
            return null;
        }
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        RequireKey(key);
        // S3 deletes are idempotent: deleting a missing object succeeds.
        return _client.DeleteObjectAsync(new DeleteObjectRequest { BucketName = _bucket, Key = key }, cancellationToken);
    }

    public async IAsyncEnumerable<StorageObjectInfo> ListAsync(
        string prefix, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        string? token = null;
        do
        {
            var response = await _client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = _bucket,
                Prefix = prefix,
                ContinuationToken = token,
            }, cancellationToken);

            foreach (var obj in response.S3Objects)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var modified = obj.LastModified ?? DateTime.UnixEpoch;
                yield return new StorageObjectInfo(obj.Key, new DateTimeOffset(modified.ToUniversalTime(), TimeSpan.Zero));
            }

            token = response.IsTruncated == true ? response.NextContinuationToken : null;
        }
        while (token is not null);
    }

    public Task<PresignedObjectUpload> CreateUploadUrlAsync(
        string key, string contentType, long contentLength, TimeSpan expiresIn, CancellationToken cancellationToken)
    {
        RequireKey(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(contentLength, 0L);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(expiresIn, TimeSpan.Zero);

        var expiresAt = DateTimeOffset.UtcNow.Add(expiresIn);
        var url = _client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = expiresAt.UtcDateTime,
            ContentType = contentType,
        });

        // The URL itself is returned to the caller only; it is never logged here.
        return Task.FromResult(new PresignedObjectUpload(url, "PUT", contentType, contentLength, expiresAt));
    }

    public Task<PresignedObjectDownload> CreateDownloadUrlAsync(
        string key, string fileName, string contentType, bool inline, TimeSpan expiresIn, CancellationToken cancellationToken)
    {
        RequireKey(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(expiresIn, TimeSpan.Zero);

        var expiresAt = DateTimeOffset.UtcNow.Add(expiresIn);
        var disposition = new ContentDisposition { FileName = fileName, Inline = inline }.ToString();
        var url = _client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = expiresAt.UtcDateTime,
            ResponseHeaderOverrides = new ResponseHeaderOverrides
            {
                ContentType = contentType,
                ContentDisposition = disposition,
            },
        });

        return Task.FromResult(new PresignedObjectDownload(url, expiresAt));
    }

    public async Task<StorageObjectHead?> HeadAsync(string key, CancellationToken cancellationToken)
    {
        RequireKey(key);
        try
        {
            var meta = await _client.GetObjectMetadataAsync(new GetObjectMetadataRequest { BucketName = _bucket, Key = key }, cancellationToken);
            return new StorageObjectHead(meta.ContentLength, meta.Headers.ContentType);
        }
        catch (AmazonS3Exception ex) when (IsNotFound(ex))
        {
            return null;
        }
    }

    public void Dispose() => _client.Dispose();

    private async Task TryDeleteAsync(string key)
    {
        try
        {
            await _client.DeleteObjectAsync(new DeleteObjectRequest { BucketName = _bucket, Key = key });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Best effort: the orphan sweep removes an object no row references.
            _logger.LogWarning("A failed object write could not be cleaned up; the orphan sweep will remove it.");
        }
    }

    private static bool IsNotFound(AmazonS3Exception ex) =>
        ex.StatusCode == HttpStatusCode.NotFound || ex.ErrorCode == LogicalNotFound;

    private static void RequireKey(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (key.StartsWith('/') || key.Contains("..", StringComparison.Ordinal) || key.Length > 1024)
        {
            throw new ArgumentException("Invalid storage key.", nameof(key));
        }
    }
}
