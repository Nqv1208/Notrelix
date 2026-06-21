using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Infrastructure.Storage;

namespace Notrelix.Infrastructure.Storage.Providers;

/// <summary>
/// Minimal local-filesystem storage provider (v4 §9). Suitable for development;
/// production should swap in an S3/R2 provider via <c>StorageRegistration</c>
/// without any change to the Application layer. Inert until an upload is invoked.
/// </summary>
public sealed class LocalStorageProvider : IStorageService
{
    private readonly StorageOptions _options;

    public LocalStorageProvider(IOptions<StorageOptions> options)
    {
        _options = options.Value;
    }

    public async Task<string> UploadFileAsync(
        Stream stream, string fileName, string contentType,
        string? folder = null, CancellationToken cancellationToken = default)
    {
        var safeName = $"{Guid.CreateVersion7():N}_{Path.GetFileName(fileName)}";
        var relativeDir = string.IsNullOrWhiteSpace(folder) ? string.Empty : folder.Trim('/');
        var targetDir = Path.Combine(_options.BasePath, relativeDir);
        Directory.CreateDirectory(targetDir);

        var fullPath = Path.Combine(targetDir, safeName);
        await using (var file = File.Create(fullPath))
        {
            await stream.CopyToAsync(file, cancellationToken);
        }

        var key = string.IsNullOrEmpty(relativeDir) ? safeName : $"{relativeDir}/{safeName}";
        return $"{_options.PublicBaseUrl.TrimEnd('/')}/{key}";
    }

    public Task DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        var prefix = _options.PublicBaseUrl.TrimEnd('/') + "/";
        if (!fileUrl.StartsWith(prefix, StringComparison.Ordinal))
            return Task.CompletedTask;

        var key = fileUrl[prefix.Length..];
        var fullPath = Path.Combine(_options.BasePath, key);
        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }

    public Task<string> GetPresignedUploadUrlAsync(
        string fileName, string contentType, TimeSpan? expiry = null,
        CancellationToken cancellationToken = default)
        // Local storage has no presigned-URL concept; a real object-store provider
        // (S3/R2) implements this. Direct server-side upload should be used instead.
        => throw new NotSupportedException(
            "Presigned uploads are not supported by LocalStorageProvider. Use an S3/R2 provider.");
}
