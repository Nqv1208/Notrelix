namespace Notrelix.Domain.Workspaces.Workspaces.Events;

[EventName("workspaces.workspace-unarchived")]
public sealed record WorkspaceUnarchivedDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid UnarchivedBy { get; }

    public WorkspaceUnarchivedDomainEvent(
        Guid accountId,
        Guid workspaceId,
        Guid unarchivedBy,
        DateTimeOffset occurredAt)
        : base(accountId, workspaceId, occurredAt)
    {
        UnarchivedBy = unarchivedBy;
    }
}
