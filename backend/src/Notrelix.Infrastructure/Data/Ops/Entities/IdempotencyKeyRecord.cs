namespace Notrelix.Infrastructure.Data.Ops.Entities;

public sealed class IdempotencyKeyRecord
{
    public Guid Id { get; private set; }
    public Guid? WorkspaceId { get; private set; }
    public Guid? UserId { get; private set; }
    public string Scope { get; private set; } = null!;
    public string IdempotencyKey { get; private set; } = null!;
    public string RequestMethod { get; private set; } = null!;
    public string RequestPath { get; private set; } = null!;
    public string RequestHash { get; private set; } = null!;
    public string Status { get; private set; } = null!;
    public int? ResponseStatusCode { get; private set; }
    public string? ResponseBodyJson { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset? LockedUntil { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    private IdempotencyKeyRecord() { }

    public static IdempotencyKeyRecord Create(
        Guid id,
        Guid? workspaceId,
        Guid? userId,
        string scope,
        string idempotencyKey,
        string requestMethod,
        string requestPath,
        string requestHash,
        DateTimeOffset expiresAt,
        DateTimeOffset createdAt)
    {
        return new IdempotencyKeyRecord
        {
            Id = id,
            WorkspaceId = workspaceId,
            UserId = userId,
            Scope = scope,
            IdempotencyKey = idempotencyKey,
            RequestMethod = requestMethod,
            RequestPath = requestPath,
            RequestHash = requestHash,
            Status = "Started",
            ExpiresAt = expiresAt,
            CreatedAt = createdAt,
        };
    }
}
