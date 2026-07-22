namespace Notrelix.Domain.Workspaces.Workspaces.Events;

public sealed record WorkspaceSettingsUpdatedDomainEvent : WorkspaceRootDomainEvent
{
    public Guid UpdatedBy { get; }

    public WorkspaceSettingsUpdatedDomainEvent(
        Guid workspaceId,
        Guid updatedBy,
        DateTimeOffset occurredAt)
        : base(workspaceId, occurredAt)
    {
        UpdatedBy = updatedBy;
    }
}
