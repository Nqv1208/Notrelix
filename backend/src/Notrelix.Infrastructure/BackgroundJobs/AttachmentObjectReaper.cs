using Notrelix.Infrastructure.Storage;
using Notrelix.Application.Features.Collaboration.Attachments.Services;

namespace Notrelix.Infrastructure.BackgroundJobs;

internal sealed record AttachmentReaperResult(int PurgeClaimed, int Purged, int OrphansDeleted, bool SweepSkipped);

/// <summary>
/// One reaper pass over attachment objects. (1) Purges the objects of stored-file attachments
/// soft-deleted longer than the retention, marking the row purged after the object is gone.
/// (2) Removes orphan objects: attachment objects older than the grace period (by the provider's
/// last-modified time, never by request state) that no attachment row references, which covers
/// the unknown-commit-outcome case the request-time compensation deliberately leaves behind.
/// Both steps are idempotent and retry safe; a failure on one object is logged and does not stop
/// the batch, and the object is retried by the next pass.
/// </summary>
internal sealed class AttachmentObjectReaper
{
    private readonly IAttachmentReaperStore _store;
    private readonly IStorageService _storage;
    private readonly IDateTimeProvider _clock;
    private readonly AttachmentRetentionOptions _options;
    private readonly ILogger<AttachmentObjectReaper> _logger;

    public AttachmentObjectReaper(
        IAttachmentReaperStore store,
        IStorageService storage,
        IDateTimeProvider clock,
        IOptions<AttachmentRetentionOptions> options,
        ILogger<AttachmentObjectReaper> logger)
    {
        _store = store;
        _storage = storage;
        _clock = clock;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AttachmentReaperResult> RunOnceAsync(CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var (claimed, purged) = await PurgeDeletedAsync(now, cancellationToken);
        var (orphans, skipped) = await SweepOrphansAsync(now, cancellationToken);

        if (purged > 0 || orphans > 0)
        {
            _logger.LogInformation(
                "Attachment object reaper removed {Purged} purged-file objects and {Orphans} orphan objects",
                purged, orphans);
        }

        return new AttachmentReaperResult(claimed, purged, orphans, skipped);
    }

    private async Task<(int Claimed, int Purged)> PurgeDeletedAsync(DateTimeOffset now, CancellationToken ct)
    {
        var cutoff = now - _options.DeletedFileRetention;
        var totalClaimed = 0;
        var totalPurged = 0;

        while (!ct.IsCancellationRequested)
        {
            var batch = await _store.PurgeDeletedAsync(cutoff, _options.BatchSize, now, DeletePurgeObjectAsync, ct);
            totalClaimed += batch.Claimed;
            totalPurged += batch.Purged;

            // Continue only while whole batches were purged; a short or failing batch ends the pass
            // (failed rows are retried by the next pass instead of being re-claimed in a tight loop).
            if (batch.Purged < _options.BatchSize)
            {
                break;
            }
        }

        return (totalClaimed, totalPurged);
    }

    private async Task<bool> DeletePurgeObjectAsync(AttachmentPurgeCandidate candidate, CancellationToken ct)
    {
        // Defence in depth: the row decides the key, so never delete outside the tenant prefix of the row.
        if (!AttachmentStorageKey.BelongsTo(candidate.StorageKey, candidate.AccountId, candidate.WorkspaceId))
        {
            _logger.LogWarning(
                "Attachment {AttachmentId} has a storage key outside its tenant prefix; object not purged",
                candidate.AttachmentId);
            return false;
        }

        try
        {
            await _storage.DeleteAsync(candidate.StorageKey, ct);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Purging the object of attachment {AttachmentId} failed; it is retried next pass", candidate.AttachmentId);
            return false;
        }
    }

    private async Task<(int Deleted, bool Skipped)> SweepOrphansAsync(DateTimeOffset now, CancellationToken ct)
    {
        var cutoff = now - _options.OrphanGracePeriod;
        var chunk = new List<string>(_options.BatchSize);
        var deleted = 0;

        async Task<bool> FlushAsync()
        {
            if (chunk.Count == 0)
            {
                return true;
            }

            var result = await _store.DeleteUnreferencedAsync(chunk.ToArray(), DeleteOrphanObjectAsync, ct);
            chunk.Clear();
            if (result is null)
            {
                return false; // another instance holds the sweep claim
            }

            deleted += result.Value;
            return true;
        }

        await foreach (var info in _storage.ListAsync(string.Empty, ct))
        {
            if (!AttachmentStorageKey.IsAttachmentKey(info.Key) || info.LastModified >= cutoff)
            {
                continue; // not an attachment object, or younger than the grace period (possibly in flight)
            }

            chunk.Add(info.Key);
            if (chunk.Count >= _options.BatchSize && !await FlushAsync())
            {
                return (deleted, true);
            }
        }

        return await FlushAsync() ? (deleted, false) : (deleted, true);
    }

    private async Task<bool> DeleteOrphanObjectAsync(string key, CancellationToken ct)
    {
        try
        {
            await _storage.DeleteAsync(key, ct);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Removing an orphan attachment object failed; it is retried next pass");
            return false;
        }
    }
}
