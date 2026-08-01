using Notrelix.Domain.Governance.ShareLinks.Events;
namespace Notrelix.Domain.Governance.ShareLinks;

public class ShareLink : AggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public ResourceType ResourceType { get; private set; }
    public Guid ResourceId { get; private set; }
    public ShareLinkTokenHash TokenHash { get; private set; } = null!;
    public ShareLinkAccessMode AccessMode { get; private set; }
    public ShareLinkStatus Status { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }

    private ShareLink() : base() { }

    public static ShareLink Create(
        Guid accountId,
        Guid workspaceId,
        ResourceType resourceType,
        Guid resourceId,
        ShareLinkTokenHash tokenHash,
        ShareLinkAccessMode accessMode,
        Guid createdBy,
        DateTimeOffset createdAt,
        DateTimeOffset? expiresAt = null)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(resourceId);
        Guard.NotNull(tokenHash);
        Guard.NotEmpty(accountId);

        if (accessMode == ShareLinkAccessMode.Public && !expiresAt.HasValue)
            throw new BusinessRuleException(GovernanceRuleCodes.Governance_ShareLink_PublicMustHaveExpiry, "Public share links must have an expiration date.");

        var link = new ShareLink
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            ResourceType = resourceType,
            ResourceId = resourceId,
            TokenHash = tokenHash,
            AccessMode = accessMode,
            Status = ShareLinkStatus.Active,
            ExpiresAt = expiresAt
        };

        link.SetAuditOnCreate(createdBy, createdAt);
        link.RaiseDomainEvent(new ShareLinkCreatedDomainEvent(accountId, workspaceId, link.Id, resourceType, resourceId, createdBy, createdAt));

        return link;
    }

    public bool IsExpired(DateTimeOffset now)
    {
        if (Status != ShareLinkStatus.Active) return true;
        if (ExpiresAt.HasValue && ExpiresAt.Value < now) return true;
        return false;
    }

    public void Disable(Guid disabledBy, DateTimeOffset disabledAt)
    {
        if (Status == ShareLinkStatus.Disabled) return;

        var pending = PrepareAuditUpdate(disabledBy, disabledAt);
        Status = ShareLinkStatus.Disabled;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new ShareLinkDisabledDomainEvent(AccountId, WorkspaceId, Id, disabledBy, disabledAt));
    }

    public void RotateTokenHash(ShareLinkTokenHash newHash, Guid rotatedBy, DateTimeOffset rotatedAt)
    {
        Guard.NotNull(newHash);

        var pending = PrepareAuditUpdate(rotatedBy, rotatedAt);
        TokenHash = newHash;
        Status = ShareLinkStatus.Active;
        ApplyAuditUpdate(pending);
        IncrementVersion();

        RaiseDomainEvent(new ShareLinkRotatedDomainEvent(AccountId, WorkspaceId, Id, rotatedBy, rotatedAt));
    }

    public void Expire(DateTimeOffset expiredAt)
    {
        if (Status != ShareLinkStatus.Active) return;

        var pending = PrepareAuditUpdate(null, expiredAt);
        Status = ShareLinkStatus.Expired;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new ShareLinkExpiredDomainEvent(AccountId, WorkspaceId, Id, expiredAt));
    }
}
