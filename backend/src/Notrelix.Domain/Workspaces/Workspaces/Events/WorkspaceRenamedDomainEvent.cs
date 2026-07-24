namespace Notrelix.Domain.Workspaces.Workspaces.Events;

[EventName("workspaces.workspace-renamed")]
public sealed record WorkspaceRenamedDomainEvent : WorkspaceScopedDomainEvent
{
    public string OldName { get; }
    public string NewName { get; }
    public Guid UpdatedBy { get; }

    public WorkspaceRenamedDomainEvent(
        Guid accountId,
        Guid workspaceId,
        string oldName,
        string newName,
        Guid updatedBy,
        DateTimeOffset occurredAt)
        : base(accountId, workspaceId, occurredAt)
    {
        OldName = oldName;
        NewName = newName;
        UpdatedBy = updatedBy;
    }
}
