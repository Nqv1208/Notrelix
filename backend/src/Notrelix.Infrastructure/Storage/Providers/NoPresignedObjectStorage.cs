namespace Notrelix.Infrastructure.Storage.Providers;

/// <summary>
/// The pre-signed capability of a provider that cannot issue URLs (the local filesystem). It reports no support, so the two-step upload and
/// the signed download are refused with a conflict and every client keeps the streaming path. It never throws from
/// <see cref="IPresignedObjectStorage.SupportsPresignedUrls"/>.
/// </summary>
public sealed class NoPresignedObjectStorage : IPresignedObjectStorage
{
    public bool SupportsPresignedUrls => false;

    public Task<PresignedObjectUpload> CreateUploadUrlAsync(string key, string contentType, long contentLength, TimeSpan expiresIn, CancellationToken cancellationToken) =>
        throw new NotSupportedException("The storage provider does not issue pre-signed URLs.");

    public Task<PresignedObjectDownload> CreateDownloadUrlAsync(string key, string fileName, string contentType, bool inline, TimeSpan expiresIn, CancellationToken cancellationToken) =>
        throw new NotSupportedException("The storage provider does not issue pre-signed URLs.");

    public Task<StorageObjectHead?> HeadAsync(string key, CancellationToken cancellationToken) =>
        throw new NotSupportedException("The storage provider does not issue pre-signed URLs.");
}
