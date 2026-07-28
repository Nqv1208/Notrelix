using Notrelix.Domain.Documents.ResourceLinks.Events;
namespace Notrelix.Domain.Documents.ResourceLinks;

public class ResourceLink : SoftDeletableAggregateRoot, IWorkspaceScoped
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
            throw new BusinessRuleException(DocumentRuleCodes.Documents_ResourceLink_CannotCreateSelfReferencing, "Cannot create a self-referencing resource link.");

        if (target.WorkspaceId.HasValue && target.WorkspaceId != source.WorkspaceId)
            throw new BusinessRuleException(DocumentRuleCodes.Documents_ResourceLink_TargetMustBeInSameWorkspace, "Target resource must belong to the same workspace as the source resource.");

        var link = new ResourceLink
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            Source = source,
            Target = target,
            Type = type
        };

        link.SetAuditOnCreate(createdBy, createdAt);
        link.RaiseDomainEvent(new ResourceLinkCreatedDomainEvent(accountId, workspaceId, source.ResourceId, target.ResourceId, type, createdAt));
        return link;
    }

    public void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        Guard.NotEmpty(deletedBy);
        if (IsDeleted) return;
        if (!MarkDeleted(deletedBy, deletedAt, reason)) return;
        var pending = PrepareAuditUpdate(deletedBy, deletedAt);
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new ResourceLinkDeletedDomainEvent(AccountId, WorkspaceId, Id, deletedAt));
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        Guard.NotEmpty(restoredBy);
        if (!IsDeleted) return;
        if (!MarkRestored(restoredBy, restoredAt)) return;
        var pending = PrepareAuditUpdate(restoredBy, restoredAt);
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new ResourceLinkRestoredDomainEvent(AccountId, WorkspaceId, Id, restoredBy, restoredAt));
    }
}
