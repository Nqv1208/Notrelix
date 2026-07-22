namespace Notrelix.Domain.Workspaces.Workspaces.Events;

public sealed record WorkspaceRenamedDomainEvent : WorkspaceRootDomainEvent
{
    public string OldName { get; }
    public string NewName { get; }
    public Guid UpdatedBy { get; }

    public WorkspaceRenamedDomainEvent(
        Guid workspaceId,
        string oldName,
        string newName,
        Guid updatedBy,
        DateTimeOffset occurredAt)
        : base(workspaceId, occurredAt)
    {
        OldName = oldName;
        NewName = newName;
        UpdatedBy = updatedBy;
    }
}
