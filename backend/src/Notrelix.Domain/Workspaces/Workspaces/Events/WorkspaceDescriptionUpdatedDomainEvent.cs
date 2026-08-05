namespace Notrelix.Domain.Workspaces.Workspaces.Events;

[EventName("workspaces.workspace-description-updated")]
public sealed record WorkspaceDescriptionUpdatedDomainEvent : WorkspaceScopedDomainEvent
{
    public string? OldDescription { get; }
    public string? NewDescription { get; }
    public Guid UpdatedBy { get; }

    public WorkspaceDescriptionUpdatedDomainEvent(
        Guid accountId,
        Guid workspaceId,
        string? oldDescription,
        string? newDescription,
        Guid updatedBy,
        DateTimeOffset occurredAt)
        : base(accountId, workspaceId, occurredAt)
    {
        OldDescription = oldDescription;
        NewDescription = newDescription;
        UpdatedBy = updatedBy;
    }
}
