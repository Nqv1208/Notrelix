using System.Buffers.Binary;
using Notrelix.Application.Common.SystemOperations;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.BackgroundJobs;

/// <summary>
/// PostgreSQL implementation of <see cref="IAvatarReaperStore"/>. The reaper has no request tenant,
/// so the transaction runs inside an explicit <see cref="SystemContextScope"/> (logged, disposed
/// after the pass) and applies the RLS worker scope through <see cref="IRlsSessionContext"/>. It reads
/// only <c>identity.users.avatar_storage_key</c> through the worker-scope SELECT policy of that table; RLS is
/// not disabled and an owner row it cannot see keeps its objects (fail closed). The sweep is elected with a
/// transaction-scoped try-advisory lock so two instances never sweep at the same time.
/// </summary>
internal sealed class PostgresAvatarReaperStore : IAvatarReaperStore
{
    private static readonly SystemOperationReason SweepReason =
        new("Identity", "Remove profile photo objects that no user references");

    private static readonly long SweepLockKey = BinaryPrimitives.ReadInt64BigEndian(
        SHA256.HashData(Encoding.UTF8.GetBytes("identity.users:avatar-orphan-sweep")));

    private readonly ApplicationDbContext _context;
    private readonly ICurrentTenantContext _tenant;
    private readonly IRlsSessionContext _rls;
    private readonly ILogger<PostgresAvatarReaperStore> _logger;

    public PostgresAvatarReaperStore(
        ApplicationDbContext context,
        ICurrentTenantContext tenant,
        IRlsSessionContext rls,
        ILogger<PostgresAvatarReaperStore> logger)
    {
        _context = context;
        _tenant = tenant;
        _rls = rls;
        _logger = logger;
    }

    public async Task<int?> DeleteUnreferencedAsync(
        IReadOnlyCollection<string> keys,
        Func<string, CancellationToken, Task<bool>> deleteObject,
        CancellationToken cancellationToken)
    {
        using var system = new SystemContextScope(_tenant, _logger, "avatar-object-reaper.orphan-sweep", SweepReason);
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        await _rls.ApplyAsync(cancellationToken);

        var claimed = await _context.Database
            .SqlQuery<bool>($"SELECT pg_try_advisory_xact_lock({SweepLockKey}) AS \"Value\"")
            .SingleAsync(cancellationToken);
        if (!claimed)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        // Fail closed: a key is removed only when its owner's row is VISIBLE to this session and that
        // row names a different object. An owner row that cannot be read (policy missing, wrong role,
        // scope not applied, deleted owner) proves nothing about references, so its objects are kept.
        var ownerIds = keys
            .Select(TryReadOwner)
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .Distinct()
            .ToArray();

        var current = (await _context.Users
                .IgnoreQueryFilters()
                .Where(u => ownerIds.Contains(u.Id))
                .Select(u => new { u.Id, u.AvatarStorageKey })
                .ToListAsync(cancellationToken))
            .ToDictionary(u => u.Id, u => u.AvatarStorageKey);

        var deleted = 0;
        var kept = 0;
        foreach (var key in keys)
        {
            var owner = TryReadOwner(key);
            if (owner is null || !current.TryGetValue(owner.Value, out var currentKey))
            {
                kept++; // owner not provable: never delete on absence of evidence
                continue;
            }

            if (!string.Equals(currentKey, key, StringComparison.Ordinal) && await deleteObject(key, cancellationToken))
            {
                deleted++;
            }
        }

        if (kept > 0)
        {
            _logger.LogWarning(
                "Avatar orphan sweep kept {Kept} objects whose owner row was not visible to the sweep", kept);
        }

        await transaction.CommitAsync(cancellationToken);
        return deleted;
    }

    private static Guid? TryReadOwner(string key)
    {
        // users/{userId:N}/avatar/{objectId}.{ext}; the shape is validated by the reaper before the call.
        const int start = 6;
        return key.Length > start + 32 && Guid.TryParseExact(key.AsSpan(start, 32), "N", out var id) ? id : null;
    }
}
