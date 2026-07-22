namespace Notrelix.Domain.Workspaces.Workspaces.Events;

public sealed record WorkspaceArchivedDomainEvent : WorkspaceRootDomainEvent
{
    public Guid ArchivedBy { get; }

    public WorkspaceArchivedDomainEvent(
        Guid workspaceId,
        Guid archivedBy,
        DateTimeOffset occurredAt)
        : base(workspaceId, occurredAt)
    {
        ArchivedBy = archivedBy;
    }
}
