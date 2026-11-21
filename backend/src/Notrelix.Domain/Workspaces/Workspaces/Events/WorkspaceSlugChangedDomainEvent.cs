namespace Notrelix.Domain.Workspaces.Workspaces.Events;

[EventName("workspaces.workspace-slug-changed")]
public sealed record WorkspaceSlugChangedDomainEvent : WorkspaceScopedDomainEvent
{
    public string OldSlug { get; }
    public string NewSlug { get; }
    public Guid UpdatedBy { get; }

    public WorkspaceSlugChangedDomainEvent(
        Guid accountId,
        Guid workspaceId,
        string oldSlug,
        string newSlug,
        Guid updatedBy,
        DateTimeOffset occurredAt)
        : base(accountId, workspaceId, occurredAt)
    {
        OldSlug = oldSlug;
        NewSlug = newSlug;
        UpdatedBy = updatedBy;
    }
}
