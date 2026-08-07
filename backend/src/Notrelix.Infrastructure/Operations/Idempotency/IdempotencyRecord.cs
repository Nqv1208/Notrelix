namespace Notrelix.Infrastructure.Operations.Idempotency;

/// <summary>
/// Durable idempotency record. Stores SHA-256 key hash, not the raw client key.
/// Unique constraint: Scope + Operation + KeyHash.
/// States: Processing | Completed. No lease model — same-transaction semantics.
/// </summary>
public sealed class IdempotencyRecord
{
    public Guid Id { get; private set; }

    public string Scope { get; private set; } = null!;
    public string Operation { get; private set; } = null!;
    public string KeyHash { get; private set; } = null!;
    public string RequestHash { get; private set; } = null!;

    public string State { get; private set; } = null!;

    public string? ResultJson { get; private set; }
    public string? ResultContract { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }

    private IdempotencyRecord() { }

    public static IdempotencyRecord CreateProcessing(
        string scope,
        string operation,
        string keyHash,
        string requestHash,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt)
    {
        return new IdempotencyRecord
        {
            Id = Guid.NewGuid(),
            Scope = scope,
            Operation = operation,
            KeyHash = keyHash,
            RequestHash = requestHash,
            State = "Processing",
            CreatedAt = createdAt,
            ExpiresAt = expiresAt,
        };
    }

    public void MarkCompleted(
        string resultJson,
        string resultContract,
        DateTimeOffset completedAt,
        DateTimeOffset expiresAt)
    {
        State = "Completed";
        ResultJson = resultJson;
        ResultContract = resultContract;
        CompletedAt = completedAt;
        ExpiresAt = expiresAt;
    }
}
