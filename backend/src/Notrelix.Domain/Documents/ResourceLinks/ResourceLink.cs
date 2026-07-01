namespace Notrelix.Domain.Documents.ResourceLinks;

public class ResourceLink : AggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public ResourceRef Source { get; private set; } = null!;
    public ResourceRef Target { get; private set; } = null!;
    public LinkType Type { get; private set; }

    private ResourceLink() : base() { }

    public static ResourceLink Create(Guid accountId, Guid workspaceId, ResourceRef source, ResourceRef target, LinkType type, Guid createdBy, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotNull(source);
        Guard.NotNull(target);

        if (source == target)
            throw new BusinessRuleException("Cannot create a self-referencing resource link.");

        if (target.WorkspaceId.HasValue && target.WorkspaceId != source.WorkspaceId)
            throw new BusinessRuleException("Target resource must belong to the same workspace as the source resource.");

        var link = new ResourceLink
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            Source = source,
            Target = target,
            Type = type
        };

        link.SetAuditOnCreate(createdBy, createdAt);
        link.AddDomainEvent(new ResourceLinkCreatedDomainEvent(accountId, workspaceId, source.ResourceId, target.ResourceId, type, createdAt));
        return link;
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        base.SoftDelete(deletedBy, deletedAt, reason);
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        AddDomainEvent(new ResourceLinkDeletedDomainEvent(AccountId, WorkspaceId, Id, deletedAt));
    }
}
