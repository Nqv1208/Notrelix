namespace Notrelix.Application.Common.Idempotency;

/// <summary>
/// Scoped idempotency identity. The raw client key is never globally unique by itself.
/// Scope is tenant-qualified via <see cref="IdempotencyPartitionFactory"/>.
/// KeyHash is the SHA-256 of the raw client key — raw key is never stored or logged.
/// </summary>
public sealed record IdempotencyIdentity(
    string Operation,
    string Scope,
    string KeyHash,
    string RequestHash);

public enum IdempotencyBeginStatus
{
    Started,
    Completed,
    PayloadMismatch
}

public sealed record IdempotencyBeginResult(
    IdempotencyBeginStatus Status,
    string? SerializedResult,
    string? ResultContract);

/// <summary>
/// Provider-independent idempotency store port.
/// Begin and Complete participate in the same uncommitted request transaction.
/// The store must NOT call SaveChanges or start a transaction — the caller owns it.
/// </summary>
public interface IIdempotencyStore
{
    Task<IdempotencyBeginResult> BeginAsync(
        IdempotencyIdentity identity,
        CancellationToken cancellationToken);

    Task CompleteAsync(
        IdempotencyIdentity identity,
        string serializedResult,
        string resultContract,
        CancellationToken cancellationToken);
}
