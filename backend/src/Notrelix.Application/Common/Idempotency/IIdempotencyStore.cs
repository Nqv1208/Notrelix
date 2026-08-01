namespace Notrelix.Application.Common.Idempotency;

/// <summary>
/// Scoped idempotency identity. The raw client key is never globally unique by itself.
/// Scope must be tenant/actor qualified (e.g. "workspace:{id}", "account:{id}", "global:user:{id}").
/// </summary>
public sealed record IdempotencyIdentity(
    string Operation,
    string Scope,
    string Key,
    string RequestHash);

public enum IdempotencyBeginStatus
{
    Started,
    Completed,
    InProgress,
    PayloadMismatch
}

public sealed record IdempotencyBeginResult(
    IdempotencyBeginStatus Status,
    Guid LeaseToken,
    string? SerializedResult,
    string? ResultContract);

/// <summary>
/// Provider-independent idempotency store port.
/// Completion must participate in the same transaction as the business mutation.
/// The store must NOT call SaveChanges — the caller owns the transaction.
/// </summary>
public interface IIdempotencyStore
{
    Task<IdempotencyBeginResult> BeginAsync(
        IdempotencyIdentity identity,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken);

    Task CompleteAsync(
        IdempotencyIdentity identity,
        Guid leaseToken,
        string serializedResult,
        string resultContract,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken);
}
