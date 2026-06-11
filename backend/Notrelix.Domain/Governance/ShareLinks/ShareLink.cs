using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Governance.Permissions;

namespace Notrelix.Domain.Governance.ShareLinks;

public class ShareLink : AuditableEntity
{
    public ResourceType ResourceType { get; private set; }
    public Guid ResourceId { get; private set; }
    public ShareLinkTokenHash TokenHash { get; private set; } = null!;
    public ShareLinkAccessMode AccessMode { get; private set; }
    public ShareLinkStatus Status { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }

    private ShareLink() : base() { }

    public static ShareLink Create(
        ResourceType resourceType,
        Guid resourceId,
        ShareLinkTokenHash tokenHash,
        ShareLinkAccessMode accessMode,
        Guid createdBy,
        DateTimeOffset? expiresAt = null)
    {
        Guard.NotEmpty(resourceId);
        Guard.NotNull(tokenHash);

        var link = new ShareLink
        {
            ResourceType = resourceType,
            ResourceId = resourceId,
            TokenHash = tokenHash,
            AccessMode = accessMode,
            Status = ShareLinkStatus.Active,
            ExpiresAt = expiresAt
        };

        link.SetAuditOnCreate(createdBy);
        link.AddDomainEvent(new ShareLinkCreatedEvent(link.Id, resourceType, resourceId, createdBy));

        return link;
    }

    public bool IsExpired()
    {
        if (Status != ShareLinkStatus.Active) return true;
        if (ExpiresAt.HasValue && ExpiresAt.Value < DateTimeOffset.UtcNow) return true;
        return false;
    }

    public void Disable(Guid disabledBy)
    {
        if (Status == ShareLinkStatus.Disabled) return;

        Status = ShareLinkStatus.Disabled;
        SetAuditOnUpdate(disabledBy);
        AddDomainEvent(new ShareLinkDisabledEvent(Id, disabledBy));
    }

    public void RotateTokenHash(ShareLinkTokenHash newHash, Guid rotatedBy)
    {
        Guard.NotNull(newHash);
        
        TokenHash = newHash;
        Status = ShareLinkStatus.Active; // Re-activate if it was expired
        SetAuditOnUpdate(rotatedBy);
        
        AddDomainEvent(new ShareLinkRotatedEvent(Id, rotatedBy));
    }
}
