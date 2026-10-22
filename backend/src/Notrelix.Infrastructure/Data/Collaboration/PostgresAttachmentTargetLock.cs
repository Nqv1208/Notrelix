using System.Buffers.Binary;
using Notrelix.Application.Features.Collaboration.Abstractions;

namespace Notrelix.Infrastructure.Data.Collaboration;

/// <summary>
/// PostgreSQL implementation of <see cref="IAttachmentTargetLock"/>. It takes
/// <c>pg_advisory_xact_lock</c> on the connection of the already-open request transaction, so the
/// lock is released exactly when that transaction commits or rolls back. It is deliberately not a
/// row lock and is requested only after an upload body was stored, so a long stream never holds
/// it and never blocks other writers of the board item. Different targets use different keys and
/// are not serialized against each other; a hash collision only over-serializes. The key space is
/// disjoint from <see cref="WorkManagement.PostgresWorkOrderingScopeLock"/> by its prefix.
/// </summary>
public sealed class PostgresAttachmentTargetLock : IAttachmentTargetLock
{
    private readonly ApplicationDbContext _context;

    public PostgresAttachmentTargetLock(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AcquireAsync(ResourceRef target, CancellationToken cancellationToken)
    {
        if (_context.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "The attachment target lock must be acquired inside the active request transaction.");
        }

        var key = ComputeKey(target);
        await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({key})", cancellationToken);
    }

    /// <summary>
    /// SHA-256 of UTF-8 "collab.attachments:&lt;resource-kind&gt;:&lt;resource-guid-N&gt;"; the first 8 bytes,
    /// read big-endian, are the signed 64-bit advisory lock key.
    /// </summary>
    public static long ComputeKey(ResourceRef target)
    {
        var hash = SHA256.HashData(
            Encoding.UTF8.GetBytes($"collab.attachments:{target.Kind.Value}:{target.ResourceId:N}"));
        return BinaryPrimitives.ReadInt64BigEndian(hash);
    }
}
