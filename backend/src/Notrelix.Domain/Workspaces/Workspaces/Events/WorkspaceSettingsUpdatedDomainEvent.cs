namespace Notrelix.Domain.Workspaces.Workspaces.Events;

[EventName("workspaces.workspace-settings-updated")]
public sealed record WorkspaceSettingsUpdatedDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid UpdatedBy { get; }

    public WorkspaceSettingsUpdatedDomainEvent(
        Guid accountId,
        Guid workspaceId,
        Guid updatedBy,
        DateTimeOffset occurredAt)
        : base(accountId, workspaceId, occurredAt)
    {
        UpdatedBy = updatedBy;
    }
}
