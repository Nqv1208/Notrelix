namespace Notrelix.Domain.Workspaces.Workspaces.Events;

[EventName("workspaces.workspace-archived")]
public sealed record WorkspaceArchivedDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid ArchivedBy { get; }

    public WorkspaceArchivedDomainEvent(
        Guid accountId,
        Guid workspaceId,
        Guid archivedBy,
        DateTimeOffset occurredAt)
        : base(accountId, workspaceId, occurredAt)
    {
        ArchivedBy = archivedBy;
    }
}
