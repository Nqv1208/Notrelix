using System.Buffers.Binary;
using Notrelix.Application.Common.SystemOperations;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.BackgroundJobs;

/// <summary>
/// PostgreSQL implementation of <see cref="IAttachmentReaperStore"/>. The reaper has no request
/// tenant, so each transaction runs inside an explicit <see cref="SystemContextScope"/> (logged,
/// disposed after the pass) and applies the RLS worker scope through <see cref="IRlsSessionContext"/>,
/// exactly as message consumers with a system scope do. It touches only <c>collab.attachments</c>;
/// RLS is not disabled and the worker policy of that table is the one that applies.
/// Claims: purge rows are locked with <c>FOR UPDATE SKIP LOCKED</c>; the orphan sweep is elected
/// with a transaction-scoped try-advisory lock so two instances never sweep at the same time.
/// </summary>
internal sealed class PostgresAttachmentReaperStore : IAttachmentReaperStore
{
    private static readonly SystemOperationReason PurgeReason =
        new("Collaboration", "Purge objects of attachments deleted longer than the retention period");

    private static readonly SystemOperationReason SweepReason =
        new("Collaboration", "Remove attachment objects that no attachment row references");

    private static readonly long OrphanSweepLockKey = BinaryPrimitives.ReadInt64BigEndian(
        SHA256.HashData(Encoding.UTF8.GetBytes("collab.attachments:orphan-sweep")));

    private readonly ApplicationDbContext _context;
    private readonly ICurrentTenantContext _tenant;
    private readonly IRlsSessionContext _rls;
    private readonly ILogger<PostgresAttachmentReaperStore> _logger;

    public PostgresAttachmentReaperStore(
        ApplicationDbContext context,
        ICurrentTenantContext tenant,
        IRlsSessionContext rls,
        ILogger<PostgresAttachmentReaperStore> logger)
    {
        _context = context;
        _tenant = tenant;
        _rls = rls;
        _logger = logger;
    }

    public async Task<AttachmentPurgeResult> PurgeDeletedAsync(
        DateTimeOffset deletedBefore,
        int batchSize,
        DateTimeOffset now,
        Func<AttachmentPurgeCandidate, CancellationToken, Task<bool>> deleteObject,
        CancellationToken cancellationToken)
    {
        using var system = new SystemContextScope(_tenant, _logger, "attachment-object-reaper.purge", PurgeReason);
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        await _rls.ApplyAsync(cancellationToken);

        var rows = await _context.Attachments
            .FromSqlInterpolated($"""
                SELECT *
                FROM collab.attachments
                WHERE deleted_at IS NOT NULL
                  AND purged_at IS NULL
                  AND storage_key IS NOT NULL
                  AND deleted_at < {deletedBefore}
                ORDER BY deleted_at
                LIMIT {batchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .IgnoreQueryFilters()
            .ToListAsync(cancellationToken);

        var purged = 0;
        foreach (var row in rows)
        {
            var candidate = new AttachmentPurgeCandidate(
                row.Id, row.AccountId, row.WorkspaceId, row.Metadata.StorageKey!);
            if (await deleteObject(candidate, cancellationToken))
            {
                row.MarkObjectPurged(now);
                purged++;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new AttachmentPurgeResult(rows.Count, purged);
    }

    public async Task<int?> DeleteUnreferencedAsync(
        IReadOnlyCollection<string> keys,
        Func<string, CancellationToken, Task<bool>> deleteObject,
        CancellationToken cancellationToken)
    {
        using var system = new SystemContextScope(_tenant, _logger, "attachment-object-reaper.orphan-sweep", SweepReason);
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        await _rls.ApplyAsync(cancellationToken);

        var claimed = await _context.Database
            .SqlQuery<bool>($"SELECT pg_try_advisory_xact_lock({OrphanSweepLockKey}) AS \"Value\"")
            .SingleAsync(cancellationToken);
        if (!claimed)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        var referenced = (await _context.Attachments
                .IgnoreQueryFilters()
                .Where(a => a.PurgedAt == null && keys.Contains(a.Metadata.StorageKey!))
                .Select(a => a.Metadata.StorageKey!)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.Ordinal);

        var deleted = 0;
        foreach (var key in keys)
        {
            if (!referenced.Contains(key) && await deleteObject(key, cancellationToken))
            {
                deleted++;
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return deleted;
    }
}
