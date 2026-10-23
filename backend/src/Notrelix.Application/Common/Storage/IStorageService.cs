namespace Notrelix.Application.Common.Storage;

/// <summary>
/// Key-centric object storage port. Callers own the object key (a server-generated,
/// tenant-prefixed key) and the metadata that references it; the provider owns the
/// bytes and the protocol. A key is an identity, never a permission: access to an
/// object is always decided by the owning use case before the provider is called, and
/// a provider never hands out a permanent public URL for protected data.
///
/// The port deliberately has no URL or signing members. A presigned direct-upload
/// flow is added later as an additional capability on a provider that supports it,
/// without changing the streaming operations below.
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Streams <paramref name="content"/> into the object at <paramref name="key"/>. The
    /// object becomes visible only when the whole stream was stored: on any failure
    /// (including an exception thrown by the stream itself, such as a size cap, or a
    /// cancellation) nothing remains at the key. Writing to an existing key fails.
    /// </summary>
    Task WriteAsync(string key, Stream content, string contentType, CancellationToken cancellationToken);

    /// <summary>Opens the object for sequential reading, or returns null when it does not exist. The caller disposes the stream.</summary>
    Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken);

    /// <summary>Deletes the object. Deleting a missing object succeeds (idempotent).</summary>
    Task DeleteAsync(string key, CancellationToken cancellationToken);

    /// <summary>
    /// Enumerates the objects whose key lies under <paramref name="prefix"/> (a slash-separated
    /// key path; empty means the whole store) with their last-modified time. The listing is
    /// key-centric and carries no content or URL. It may be enumerated while objects are written
    /// or deleted, so a caller must treat it as a best-effort snapshot, and must base any
    /// age-driven decision on <see cref="StorageObjectInfo.LastModified"/>, never on request state.
    /// A partially written object (a write in flight) is reported with its latest modification time.
    /// </summary>
    IAsyncEnumerable<StorageObjectInfo> ListAsync(string prefix, CancellationToken cancellationToken);
}

/// <summary>An object key and when the provider last modified it (UTC).</summary>
public sealed record StorageObjectInfo(string Key, DateTimeOffset LastModified);
