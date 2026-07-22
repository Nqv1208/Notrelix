namespace Notrelix.Domain.Workspaces.Workspaces.Events;

public sealed record WorkspaceUnarchivedDomainEvent : WorkspaceRootDomainEvent
{
    public Guid UnarchivedBy { get; }

    public WorkspaceUnarchivedDomainEvent(
        Guid workspaceId,
        Guid unarchivedBy,
        DateTimeOffset occurredAt)
        : base(workspaceId, occurredAt)
    {
        UnarchivedBy = unarchivedBy;
    }
}
