namespace Notrelix.Infrastructure.Storage;

/// <summary>
/// Options for file/object storage. The database stores metadata and a storage
/// key; binaries live in the storage provider. See
/// <c>backend/docs/architecture/infrastructure-and-data.md</c>.
/// </summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>Local filesystem root used by <c>LocalStorageProvider</c>.</summary>
    public string BasePath { get; init; } = "storage";

    /// <summary>Public base URL prepended to returned object keys.</summary>
    public string PublicBaseUrl { get; init; } = "/files";
}
