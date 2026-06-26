namespace Notrelix.Domain.Documents.ResourceLinks;

public class ResourceLink : AggregateRoot, IWorkspaceScoped
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

        if (target.WorkspaceId.HasValue && target.WorkspaceId != source.WorkspaceId)
            throw new BusinessRuleException("Target resource must belong to the same workspace as the source resource.");

        var link = new ResourceLink
        {
            WorkspaceId = workspaceId,
            Source = source,
            Target = target,
            Type = type
        };

        link.SetAuditOnCreate(createdBy, createdAt);
        link.AddDomainEvent(new ResourceLinkCreatedDomainEvent(workspaceId, source.ResourceId, target.ResourceId, type, createdAt));
        return link;
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        base.SoftDelete(deletedBy, deletedAt, reason);
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        AddDomainEvent(new ResourceLinkDeletedDomainEvent(WorkspaceId, Id, deletedAt));
    }
}
