using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Domain.Identity.Tokens;

public class ApiToken : AggregateRoot, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public Guid? UserId { get; private set; }
    public string Name { get; private set; } = null!;
    public string TokenHash { get; private set; } = null!;
    public string ScopesJson { get; private set; } = "[]";
    public ApiTokenStatus Status { get; private set; } = ApiTokenStatus.Active;
    public DateTimeOffset? LastUsedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public Guid? RevokedBy { get; private set; }

    private ApiToken() : base() { }

    public static ApiToken Create(
        Guid workspaceId,
        Guid? userId,
        string name,
        string tokenHash,
        string? scopesJson,
        Guid createdBy,
        DateTimeOffset createdAt,
        DateTimeOffset? expiresAt = null)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotNullOrWhiteSpace(name);
        Guard.NotNullOrWhiteSpace(tokenHash);

        var token = new ApiToken
        {
            WorkspaceId = workspaceId,
            UserId = userId,
            Name = name.Trim(),
            TokenHash = tokenHash,
            ScopesJson = scopesJson ?? "[]",
            Status = ApiTokenStatus.Active,
            ExpiresAt = expiresAt
        };

        token.SetAuditOnCreate(createdBy, createdAt);
        token.AddDomainEvent(new ApiTokenCreatedDomainEvent(workspaceId, token.Id, name, createdBy, createdAt));
        return token;
    }

    public void Revoke(Guid revokedBy, DateTimeOffset revokedAt)
    {
        EnsureNotDeleted();
        if (Status == ApiTokenStatus.Revoked) return;

        Status = ApiTokenStatus.Revoked;
        RevokedAt = revokedAt;
        RevokedBy = revokedBy;
        SetAuditOnUpdate(revokedBy, revokedAt);
        AddDomainEvent(new ApiTokenRevokedDomainEvent(WorkspaceId, Id, revokedBy, revokedAt));
        IncrementVersion();
    }

    public void RecordUse(DateTimeOffset usedAt)
    {
        EnsureNotDeleted();
        if (ExpiresAt.HasValue && usedAt > ExpiresAt.Value)
        {
            Status = ApiTokenStatus.Expired;
            throw new BusinessRuleException("Cannot use an expired API token.");
        }

        if (Status != ApiTokenStatus.Active)
            throw new BusinessRuleException("Cannot use an inactive API token.");

        LastUsedAt = usedAt;
        IncrementVersion();
    }
}
