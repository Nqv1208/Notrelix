using System.Buffers.Binary;
using Notrelix.Application.Features.WorkManagement.Ordering;

namespace Notrelix.Infrastructure.Data.WorkManagement;

/// <summary>
/// PostgreSQL implementation of <see cref="IWorkOrderingScopeLock"/>
/// (WM-V3-ORDER-001). It takes <c>pg_advisory_xact_lock</c> on the connection of
/// the already-open request transaction, so the lock is released exactly when that
/// transaction commits or rolls back. Different scopes use different keys and are
/// not serialized against each other; a hash collision only over-serializes.
/// </summary>
public sealed class PostgresWorkOrderingScopeLock : IWorkOrderingScopeLock
{
    private readonly ApplicationDbContext _context;

    public PostgresWorkOrderingScopeLock(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AcquireAsync(WorkOrderingScope scope, CancellationToken cancellationToken)
    {
        if (_context.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                $"The {scope.KindName} ordering lock must be acquired inside the active request transaction.");
        }

        var key = ComputeKey(scope);
        await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({key})", cancellationToken);
    }

    /// <summary>
    /// SHA-256 of UTF-8 "&lt;scope-kind&gt;:&lt;parent-guid-N&gt;"; the first 8 bytes,
    /// read big-endian, are the signed 64-bit advisory lock key (SPEC WM-V3-DEC-001).
    /// </summary>
    public static long ComputeKey(WorkOrderingScope scope)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{scope.KindName}:{scope.ParentId:N}"));
        return BinaryPrimitives.ReadInt64BigEndian(hash);
    }
}
