namespace Notrelix.Domain.Governance.ShareLinks;

public class ShareLink : AggregateRoot, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public ResourceType ResourceType { get; private set; }
    public Guid ResourceId { get; private set; }
    public ShareLinkTokenHash TokenHash { get; private set; } = null!;
    public ShareLinkAccessMode AccessMode { get; private set; }
    public ShareLinkStatus Status { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }

    private ShareLink() : base() { }

    public static ShareLink Create(
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

        if (accessMode == ShareLinkAccessMode.Public && !expiresAt.HasValue)
            throw new BusinessRuleException("Public share links must have an expiration date.");

        var link = new ShareLink
        {
            WorkspaceId = workspaceId,
            ResourceType = resourceType,
            ResourceId = resourceId,
            TokenHash = tokenHash,
            AccessMode = accessMode,
            Status = ShareLinkStatus.Active,
            ExpiresAt = expiresAt
        };

        link.SetAuditOnCreate(createdBy, createdAt);
        link.AddDomainEvent(new ShareLinkCreatedEvent(workspaceId, link.Id, resourceType, resourceId, createdBy, createdAt));

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

        Status = ShareLinkStatus.Disabled;
        SetAuditOnUpdate(disabledBy, disabledAt);
        IncrementVersion();
        AddDomainEvent(new ShareLinkDisabledEvent(WorkspaceId, Id, disabledBy, disabledAt));
    }

    public void RotateTokenHash(ShareLinkTokenHash newHash, Guid rotatedBy, DateTimeOffset rotatedAt)
    {
        Guard.NotNull(newHash);

        TokenHash = newHash;
        Status = ShareLinkStatus.Active;
        SetAuditOnUpdate(rotatedBy, rotatedAt);
        IncrementVersion();

        AddDomainEvent(new ShareLinkRotatedEvent(WorkspaceId, Id, rotatedBy, rotatedAt));
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        base.SoftDelete(deletedBy, deletedAt, reason);
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        AddDomainEvent(new ShareLinkSoftDeletedEvent(WorkspaceId, Id, deletedBy, deletedAt));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        base.Restore(restoredBy, restoredAt);
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        AddDomainEvent(new ShareLinkRestoredEvent(WorkspaceId, Id, restoredBy, restoredAt));
    }

    public void Expire(DateTimeOffset expiredAt)
    {
        if (Status != ShareLinkStatus.Active) return;

        Status = ShareLinkStatus.Expired;
        SetAuditOnUpdate(null, expiredAt);
        IncrementVersion();
        AddDomainEvent(new ShareLinkExpiredEvent(WorkspaceId, Id, expiredAt));
    }
}
