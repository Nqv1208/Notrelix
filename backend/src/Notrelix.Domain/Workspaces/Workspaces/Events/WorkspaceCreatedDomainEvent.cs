namespace Notrelix.Domain.Workspaces.Workspaces.Events;

[EventName("workspaces.workspace-created")]
public sealed record WorkspaceCreatedDomainEvent : WorkspaceScopedDomainEvent
{
    public string Name { get; }
    public string Slug { get; }
    public Guid CreatedBy { get; }

    public WorkspaceCreatedDomainEvent(
        Guid accountId,
        Guid workspaceId,
        string name,
        string slug,
        Guid createdBy,
        DateTimeOffset occurredAt)
        : base(accountId, workspaceId, occurredAt)
    {
        Name = name;
        Slug = slug;
        CreatedBy = createdBy;
    }
}
