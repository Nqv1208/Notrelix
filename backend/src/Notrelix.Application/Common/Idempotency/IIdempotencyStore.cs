namespace Notrelix.Application.Common.Idempotency;

/// <summary>
/// Scoped idempotency identity. The raw client key is never globally unique by itself.
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
    string LeaseToken,
    string? SerializedResult,
    string? ResultType);

/// <summary>
/// Provider-independent idempotency store port.
/// Completion must participate in the same transaction as the business mutation.
/// </summary>
public interface IIdempotencyStore
{
    Task<IdempotencyBeginResult> BeginAsync(
        IdempotencyIdentity identity,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken);

    Task CompleteAsync(
        IdempotencyIdentity identity,
        string leaseToken,
        string serializedResult,
        string resultType,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken);
}
