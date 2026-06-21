namespace Notrelix.Infrastructure.Storage;

/// <summary>
/// Options for file/object storage (v4 §9). The DB only stores metadata +
/// storage key; binaries live in the storage provider.
/// </summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>Local filesystem root used by <c>LocalStorageProvider</c>.</summary>
    public string BasePath { get; init; } = "storage";

    /// <summary>Public base URL prepended to returned object keys.</summary>
    public string PublicBaseUrl { get; init; } = "/files";
}
