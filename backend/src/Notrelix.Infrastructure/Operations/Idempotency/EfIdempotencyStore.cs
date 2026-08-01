using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Idempotency;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Operations.Idempotency;

namespace Notrelix.Infrastructure.Operations.Idempotency;

/// <summary>
/// EF Core implementation of the Application idempotency store.
/// Uses the scoped ApplicationDbContext — participates in the request transaction.
/// Does NOT call SaveChanges; the caller owns the transaction boundary.
/// </summary>
public sealed class EfIdempotencyStore : IIdempotencyStore
{
    private readonly ApplicationDbContext _context;
    private readonly TimeProvider _timeProvider;

    public EfIdempotencyStore(ApplicationDbContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public async Task<IdempotencyBeginResult> BeginAsync(
        IdempotencyIdentity identity,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var keyHash = HashKey(identity.Key);

        var existing = await _context.Set<IdempotencyRecord>()
            .FirstOrDefaultAsync(r =>
                r.Scope == identity.Scope
                && r.Operation == identity.Operation
                && r.KeyHash == keyHash,
                cancellationToken);

        if (existing is null)
        {
            var leaseToken = Guid.NewGuid();
            var record = IdempotencyRecord.CreateProcessing(
                identity.Scope,
                identity.Operation,
                keyHash,
                identity.RequestHash,
                leaseToken,
                now.Add(leaseDuration),
                now);

            _context.Set<IdempotencyRecord>().Add(record);

            return new IdempotencyBeginResult(
                IdempotencyBeginStatus.Started,
                leaseToken,
                null,
                null);
        }

        if (existing.State == "Completed")
        {
            if (existing.ExpiresAt <= now)
            {
                // Expired completed record — treat as new
                _context.Set<IdempotencyRecord>().Remove(existing);
                var leaseToken = Guid.NewGuid();
                var record = IdempotencyRecord.CreateProcessing(
                    identity.Scope, identity.Operation, keyHash,
                    identity.RequestHash, leaseToken, now.Add(leaseDuration), now);
                _context.Set<IdempotencyRecord>().Add(record);
                return new IdempotencyBeginResult(IdempotencyBeginStatus.Started, leaseToken, null, null);
            }

            if (existing.RequestHash != identity.RequestHash)
            {
                return new IdempotencyBeginResult(
                    IdempotencyBeginStatus.PayloadMismatch, Guid.Empty, null, null);
            }

            return new IdempotencyBeginResult(
                IdempotencyBeginStatus.Completed,
                Guid.Empty,
                existing.ResultJson,
                existing.ResultContract);
        }

        // State == "Processing"
        if (existing.LeaseExpiresAt > now)
        {
            var retryAfter = existing.LeaseExpiresAt - now;
            return new IdempotencyBeginResult(
                IdempotencyBeginStatus.InProgress, Guid.Empty, null, null);
        }

        // Lease expired — reclaim
        var newLeaseToken = Guid.NewGuid();
        existing.ReclaimLease(newLeaseToken, now.Add(leaseDuration));

        return new IdempotencyBeginResult(
            IdempotencyBeginStatus.Started,
            newLeaseToken,
            null,
            null);
    }

    public async Task CompleteAsync(
        IdempotencyIdentity identity,
        Guid leaseToken,
        string serializedResult,
        string resultContract,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var keyHash = HashKey(identity.Key);

        var record = await _context.Set<IdempotencyRecord>()
            .FirstOrDefaultAsync(r =>
                r.Scope == identity.Scope
                && r.Operation == identity.Operation
                && r.KeyHash == keyHash,
                cancellationToken);

        if (record is null)
        {
            throw new InvalidOperationException(
                $"Idempotency record not found for operation '{identity.Operation}' during completion.");
        }

        if (record.State != "Processing" || record.LeaseToken != leaseToken)
        {
            throw new InvalidOperationException(
                $"Idempotency lease mismatch for operation '{identity.Operation}'. " +
                "The lease may have expired and been reclaimed by another worker.");
        }

        record.MarkCompleted(serializedResult, resultContract, now, expiresAt);
    }

    private static string HashKey(string rawKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
