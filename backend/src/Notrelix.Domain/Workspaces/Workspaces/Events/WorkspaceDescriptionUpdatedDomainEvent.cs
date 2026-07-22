namespace Notrelix.Domain.Workspaces.Workspaces.Events;

public sealed record WorkspaceDescriptionUpdatedDomainEvent : WorkspaceRootDomainEvent
{
    public string? OldDescription { get; }
    public string? NewDescription { get; }
    public Guid UpdatedBy { get; }

    public WorkspaceDescriptionUpdatedDomainEvent(
        Guid workspaceId,
        string? oldDescription,
        string? newDescription,
        Guid updatedBy,
        DateTimeOffset occurredAt)
        : base(workspaceId, occurredAt)
    {
        OldDescription = oldDescription;
        NewDescription = newDescription;
        UpdatedBy = updatedBy;
    }
}
