using System.Runtime.CompilerServices;

namespace Notrelix.Infrastructure.Storage.Providers;

/// <summary>
/// Local-filesystem implementation of <see cref="IStorageService"/>. Suitable for development
/// and single-node deployments; an S3/R2 provider replaces it behind <c>StorageRegistration</c>
/// without changing the Application layer. See
/// <c>backend/docs/architecture/infrastructure-and-data.md</c>.
///
/// Keys are server-generated, slash-separated and validated here as well (defence in depth):
/// every segment must be a plain name, so no key can escape <see cref="StorageOptions.BasePath"/>.
/// The provider never produces a URL; objects are served only through authorised API operations.
/// </summary>
public sealed class LocalStorageProvider : IStorageService
{
    private const int MaxKeyLength = 1024;
    private const string PartialSuffix = ".part";

    private readonly string _root;

    public LocalStorageProvider(IOptions<StorageOptions> options)
    {
        _root = Path.GetFullPath(options.Value.BasePath);
    }

    public async Task WriteAsync(string key, Stream content, string contentType, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        var finalPath = ResolvePath(key);
        var partialPath = finalPath + PartialSuffix;

        Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);
        if (File.Exists(finalPath))
        {
            throw new IOException("An object already exists at the requested key.");
        }

        try
        {
            await using (var file = new FileStream(
                partialPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true))
            {
                await content.CopyToAsync(file, 81920, cancellationToken);
                await file.FlushAsync(cancellationToken);
            }

            File.Move(partialPath, finalPath, overwrite: false);
        }
        catch
        {
            // Nothing may remain at the key (nor beside it) after a failed or aborted write.
            TryDelete(partialPath);
            TryDelete(finalPath);
            throw;
        }
    }

    public Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken)
    {
        var path = ResolvePath(key);
        if (!File.Exists(path))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(
            path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 81920, useAsync: true);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        var path = ResolvePath(key);
        TryDelete(path + PartialSuffix);
        TryDelete(path);
        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<StorageObjectInfo> ListAsync(
        string prefix, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        var start = prefix.Length == 0 ? _root : ResolvePath(prefix);
        if (!Directory.Exists(start))
        {
            yield break;
        }

        var options = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true };
        foreach (var path in Directory.EnumerateFiles(start, "*", options))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relative = Path.GetRelativePath(_root, path).Replace(Path.DirectorySeparatorChar, '/');
            var key = relative.EndsWith(PartialSuffix, StringComparison.Ordinal)
                ? relative[..^PartialSuffix.Length]
                : relative;
            if (!IsValidKey(key))
            {
                continue; // foreign file that no provider operation could have produced
            }

            DateTimeOffset modified;
            try
            {
                modified = new DateTimeOffset(File.GetLastWriteTimeUtc(path), TimeSpan.Zero);
            }
            catch (IOException)
            {
                continue; // deleted while listing
            }

            yield return new StorageObjectInfo(key, modified);
            await Task.Yield();
        }
    }

    private static bool IsValidKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > MaxKeyLength)
        {
            return false;
        }

        foreach (var segment in key.Split('/'))
        {
            if (segment.Length == 0 || segment is "." or ".."
                || !segment.All(static c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.'))
            {
                return false;
            }
        }

        return true;
    }

    private string ResolvePath(string key)
    {
        if (!IsValidKey(key))
        {
            throw new ArgumentException("Invalid storage key.", nameof(key));
        }

        var segments = key.Split('/');
        var full = Path.GetFullPath(Path.Combine(_root, Path.Combine(segments)));
        if (!full.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new ArgumentException("Invalid storage key.", nameof(key));
        }

        return full;
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // Best effort: a leftover temp file is unreachable (no row references it).
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
