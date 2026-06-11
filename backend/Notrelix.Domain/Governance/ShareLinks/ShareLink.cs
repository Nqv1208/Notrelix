using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Governance.Permissions;

namespace Notrelix.Domain.Governance.ShareLinks;

public class ShareLink : AuditableEntity
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
        AddDomainEvent(new ShareLinkDisabledEvent(WorkspaceId, Id, disabledBy, disabledAt));
    }

    public void RotateTokenHash(ShareLinkTokenHash newHash, Guid rotatedBy, DateTimeOffset rotatedAt)
    {
        Guard.NotNull(newHash);
        
        TokenHash = newHash;
        Status = ShareLinkStatus.Active; // Re-activate if it was expired
        SetAuditOnUpdate(rotatedBy, rotatedAt);
        
        AddDomainEvent(new ShareLinkRotatedEvent(WorkspaceId, Id, rotatedBy, rotatedAt));
    }
}
