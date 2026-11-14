namespace Notrelix.Application.Common.Storage;

/// <summary>
/// Optional capability of an object storage provider: short-lived pre-signed URLs, so large bytes travel directly between the client and the
/// bucket instead of through the API. Only a provider that reports <see cref="SupportsPresignedUrls"/> true may be asked for a URL; the
/// authorization of every use is still decided by the owning use case before a URL is created, and a URL is never logged or cached.
/// The filesystem provider reports false, so development and tests keep the streaming path.
/// </summary>
public interface IPresignedObjectStorage
{
    /// <summary>True when this provider can issue pre-signed URLs. When false the other members throw <see cref="NotSupportedException"/>.</summary>
    bool SupportsPresignedUrls { get; }

    /// <summary>
    /// A pre-signed PUT for the object at <paramref name="key"/>. The URL is bound to <paramref name="contentType"/> and expires after
    /// <paramref name="expiresIn"/>. The declared <paramref name="contentLength"/> is returned for the client and re-verified at commit.
    /// </summary>
    Task<PresignedObjectUpload> CreateUploadUrlAsync(
        string key, string contentType, long contentLength, TimeSpan expiresIn, CancellationToken cancellationToken);

    /// <summary>
    /// A pre-signed GET for the object at <paramref name="key"/>. The response carries <paramref name="contentType"/> and a
    /// Content-Disposition for <paramref name="fileName"/> (inline when <paramref name="inline"/>, else attachment).
    /// </summary>
    Task<PresignedObjectDownload> CreateDownloadUrlAsync(
        string key, string fileName, string contentType, bool inline, TimeSpan expiresIn, CancellationToken cancellationToken);

    /// <summary>The size and content type the provider stored for the object, or null when no object exists at the key.</summary>
    Task<StorageObjectHead?> HeadAsync(string key, CancellationToken cancellationToken);
}

/// <summary>A pre-signed upload: the client sends the bytes with <see cref="Method"/> to <see cref="Url"/> before <see cref="ExpiresAt"/>.</summary>
public sealed record PresignedObjectUpload(string Url, string Method, string ContentType, long ContentLength, DateTimeOffset ExpiresAt);

/// <summary>A pre-signed download, valid until <see cref="ExpiresAt"/>.</summary>
public sealed record PresignedObjectDownload(string Url, DateTimeOffset ExpiresAt);

/// <summary>The facts the provider stored for an object: byte size and the content type it was written with.</summary>
public sealed record StorageObjectHead(long SizeBytes, string? ContentType);
