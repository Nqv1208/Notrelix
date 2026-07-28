using Notrelix.Domain.Identity.Tokens.Events;

namespace Notrelix.Domain.Identity.Tokens;

public class ApiToken : SoftDeletableAggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid? UserId { get; private set; }
    public string Name { get; private set; } = null!;
    public string TokenHash { get; private set; } = null!;
    public ApiTokenScopes? Scopes { get; private set; }
    public ApiTokenStatus Status { get; private set; } = ApiTokenStatus.Active;
    public DateTimeOffset? LastUsedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public Guid? RevokedBy { get; private set; }

    private ApiToken() : base() { }

    public static ApiToken Create(
        Guid accountId,
        Guid workspaceId,
        Guid? userId,
        string name,
        string tokenHash,
        ApiTokenScopes? scopes,
        Guid createdBy,
        DateTimeOffset createdAt,
        DateTimeOffset? expiresAt = null)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotNullOrWhiteSpace(name);
        Guard.NotNullOrWhiteSpace(tokenHash);
        Guard.NotEmpty(accountId);

        var token = new ApiToken
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            UserId = userId,
            Name = name.Trim(),
            TokenHash = tokenHash,
            Scopes = scopes,
            Status = ApiTokenStatus.Active,
            ExpiresAt = expiresAt
        };

        token.SetAuditOnCreate(createdBy, createdAt);
        token.RaiseDomainEvent(new ApiTokenCreatedDomainEvent(accountId, workspaceId, token.Id, name, createdAt));
        return token;
    }

    public void Revoke(Guid revokedBy, DateTimeOffset revokedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(revokedBy);
        if (Status == ApiTokenStatus.Revoked) return;

        var pending = PrepareAuditUpdate(revokedBy, revokedAt);
        Status = ApiTokenStatus.Revoked;
        RevokedAt = revokedAt;
        RevokedBy = revokedBy;
        ApplyAuditUpdate(pending);
        RaiseDomainEvent(new ApiTokenRevokedDomainEvent(AccountId, WorkspaceId, Id, revokedAt));
        IncrementVersion();
    }

    public void RecordUse(DateTimeOffset usedAt)
    {
        EnsureNotDeleted();

        if (Status != ApiTokenStatus.Active)
            throw new BusinessRuleException(IdentityRuleCodes.Identity_ApiToken_CannotUseInactive, "Cannot use an inactive API token.");

        if (ExpiresAt.HasValue && usedAt > ExpiresAt.Value)
        {
            Status = ApiTokenStatus.Expired;
            IncrementVersion();
            throw new BusinessRuleException(IdentityRuleCodes.Identity_ApiToken_CannotUseExpired, "Cannot use an expired API token.");
        }

        var pending = PrepareAuditUpdate(AccountId, usedAt);
        LastUsedAt = usedAt;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new ApiTokenRecordedUseDomainEvent(AccountId, WorkspaceId, Id, usedAt));
    }

    public void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        Guard.NotEmpty(deletedBy);
        if (IsDeleted) return;
        var pendingDeletion = PrepareDeletion(deletedBy, deletedAt, reason);
        ApplyDeletion(pendingDeletion);
        IncrementVersion();
        RaiseDomainEvent(new ApiTokenSoftDeletedDomainEvent(AccountId, WorkspaceId, Id, deletedAt));
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        Guard.NotEmpty(restoredBy);
        if (!IsDeleted) return;
        var pendingRestore = PrepareRestore(restoredBy, restoredAt);
        ApplyRestore(pendingRestore);
        IncrementVersion();
        RaiseDomainEvent(new ApiTokenRestoredDomainEvent(AccountId, WorkspaceId, Id, restoredAt));
    }
}
