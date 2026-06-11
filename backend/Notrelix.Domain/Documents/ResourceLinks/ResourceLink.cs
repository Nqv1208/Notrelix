using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Domain.Documents.ResourceLinks;

public class ResourceLink : SoftDeletableEntity
{
    public Guid WorkspaceId { get; private set; }
    public ResourceRef Source { get; private set; } = null!;
    public ResourceRef Target { get; private set; } = null!;
    public LinkType Type { get; private set; }

    private ResourceLink() : base() { }

    public static ResourceLink Create(Guid workspaceId, ResourceRef source, ResourceRef target, LinkType type, Guid createdBy, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotNull(source);
        Guard.NotNull(target);

        if (source == target)
            throw new BusinessRuleException("Cannot create a self-referencing resource link.");

        var link = new ResourceLink
        {
            WorkspaceId = workspaceId,
            Source = source,
            Target = target,
            Type = type
        };

        link.SetAuditOnCreate(createdBy, createdAt);
        link.AddDomainEvent(new ResourceLinkCreatedEvent(workspaceId, source.ResourceId, target.ResourceId, type, createdAt));
        return link;
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        base.SoftDelete(deletedBy, deletedAt, reason);
        AddDomainEvent(new ResourceLinkDeletedEvent(WorkspaceId, Id, deletedAt));
    }
}
