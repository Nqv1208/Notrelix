namespace Notrelix.Domain.Common;

public abstract record WorkspaceRootDomainEvent : DomainEvent
{
    public Guid WorkspaceId { get; }

    protected WorkspaceRootDomainEvent(
        Guid workspaceId,
        DateTimeOffset occurredAt)
        : base(occurredAt)
    {
        WorkspaceId = workspaceId;
    }
}
