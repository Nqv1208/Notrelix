using Notrelix.Application.Features.Identity.Profiles.Services;
using Notrelix.Infrastructure.Storage;

namespace Notrelix.Infrastructure.BackgroundJobs;

/// <summary>
/// One reaper pass over profile photo objects. A replaced or removed photo leaves its object
/// unreferenced (the request cannot delete it safely before commit), and a crash after the write
/// leaves an object that no row ever referenced. This pass removes the avatar objects older than the
/// grace period (by the provider's last-modified time, never by request state) that no user row
/// references. It is idempotent and retry safe: a failure on one object is logged and the object is
/// retried by the next pass. The retention policy is shared with the attachment reaper
/// (<see cref="AttachmentRetentionOptions"/>): the grace period must exceed the longest upload.
/// </summary>
internal sealed class AvatarObjectReaper
{
    private readonly IAvatarReaperStore _store;
    private readonly IStorageService _storage;
    private readonly IDateTimeProvider _clock;
    private readonly AttachmentRetentionOptions _options;
    private readonly ILogger<AvatarObjectReaper> _logger;

    public AvatarObjectReaper(
        IAvatarReaperStore store,
        IStorageService storage,
        IDateTimeProvider clock,
        IOptions<AttachmentRetentionOptions> options,
        ILogger<AvatarObjectReaper> logger)
    {
        _store = store;
        _storage = storage;
        _clock = clock;
        _options = options.Value;
        _logger = logger;
    }

    /// <returns>The number of orphan objects deleted, and whether the pass was skipped because another instance sweeps.</returns>
    public async Task<(int Deleted, bool Skipped)> RunOnceAsync(CancellationToken cancellationToken)
    {
        var cutoff = _clock.UtcNow - _options.OrphanGracePeriod;
        var chunk = new List<string>(_options.BatchSize);
        var deleted = 0;

        async Task<bool> FlushAsync()
        {
            if (chunk.Count == 0)
            {
                return true;
            }

            var result = await _store.DeleteUnreferencedAsync(chunk.ToArray(), DeleteObjectAsync, cancellationToken);
            chunk.Clear();
            if (result is null)
            {
                return false; // another instance holds the sweep claim
            }

            deleted += result.Value;
            return true;
        }

        await foreach (var info in _storage.ListAsync(AvatarStorageKey.ListPath, cancellationToken))
        {
            if (!AvatarStorageKey.IsAvatarKey(info.Key) || info.LastModified >= cutoff)
            {
                continue; // not an avatar object, or younger than the grace period (possibly in flight)
            }

            chunk.Add(info.Key);
            if (chunk.Count >= _options.BatchSize && !await FlushAsync())
            {
                return (deleted, true);
            }
        }

        var completed = await FlushAsync();
        if (deleted > 0)
        {
            _logger.LogInformation("Avatar object reaper removed {Deleted} unreferenced objects", deleted);
        }

        return (deleted, !completed);
    }

    private async Task<bool> DeleteObjectAsync(string key, CancellationToken ct)
    {
        try
        {
            await _storage.DeleteAsync(key, ct);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Removing an unreferenced avatar object failed; it is retried next pass");
            return false;
        }
    }
}
