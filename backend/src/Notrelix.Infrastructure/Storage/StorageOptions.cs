namespace Notrelix.Infrastructure.Storage;

/// <summary>
/// Options for file/object storage. The database stores metadata and a storage
/// key; binaries live in the storage provider. See
/// <c>backend/docs/architecture/infrastructure-and-data.md</c>.
/// </summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>
    /// The provider that stores the bytes: <c>FileSystem</c> (default; development and tests, streams through the API) or <c>S3</c>
    /// (an S3-compatible bucket; supports pre-signed URLs).
    /// </summary>
    public string Provider { get; init; } = StorageProviderNames.FileSystem;

    /// <summary>Local filesystem root used by <c>LocalStorageProvider</c>.</summary>
    public string BasePath { get; init; } = "storage";

    /// <summary>Bucket settings, used only when <see cref="Provider"/> is <c>S3</c>.</summary>
    public S3StorageOptions S3 { get; init; } = new();
}

public static class StorageProviderNames
{
    public const string FileSystem = "FileSystem";
    public const string S3 = "S3";
}

/// <summary>
/// S3-compatible bucket settings (AWS S3, MinIO, Cloudflare R2). The access key pair is optional: when absent the SDK default credential
/// chain applies. The secret is never logged and never returned to a client.
/// </summary>
public sealed class S3StorageOptions
{
    /// <summary>The private bucket that holds every object. Required for the S3 provider.</summary>
    public string Bucket { get; init; } = string.Empty;

    /// <summary>Endpoint of an S3-compatible service. Empty for AWS S3 itself.</summary>
    public string ServiceUrl { get; init; } = string.Empty;

    public string Region { get; init; } = "us-east-1";

    /// <summary>Path-style addressing, required by MinIO and most self-hosted S3-compatible services.</summary>
    public bool ForcePathStyle { get; init; } = true;

    public string AccessKeyId { get; init; } = string.Empty;

    public string SecretAccessKey { get; init; } = string.Empty;
}

public sealed class StorageOptionsValidator : IValidateOptions<StorageOptions>
{
    public ValidateOptionsResult Validate(string? name, StorageOptions options)
    {
        var failures = new List<string>();
        if (string.IsNullOrWhiteSpace(options.BasePath))
            failures.Add($"{StorageOptions.SectionName}:BasePath must not be empty.");

        if (options.Provider is not (StorageProviderNames.FileSystem or StorageProviderNames.S3))
            failures.Add($"{StorageOptions.SectionName}:Provider must be {StorageProviderNames.FileSystem} or {StorageProviderNames.S3}.");

        if (options.Provider == StorageProviderNames.S3)
        {
            if (string.IsNullOrWhiteSpace(options.S3.Bucket))
                failures.Add($"{StorageOptions.SectionName}:S3:Bucket is required for the S3 provider.");
            if (string.IsNullOrWhiteSpace(options.S3.AccessKeyId) != string.IsNullOrWhiteSpace(options.S3.SecretAccessKey))
                failures.Add($"{StorageOptions.SectionName}:S3:AccessKeyId and SecretAccessKey must be set together.");
            var serviceUrlValid = Uri.TryCreate(options.S3.ServiceUrl, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https";
            if (!string.IsNullOrWhiteSpace(options.S3.ServiceUrl) && !serviceUrlValid)
                failures.Add($"{StorageOptions.SectionName}:S3:ServiceUrl must be an absolute http or https URL.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
