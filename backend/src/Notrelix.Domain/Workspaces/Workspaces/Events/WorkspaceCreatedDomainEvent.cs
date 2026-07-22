namespace Notrelix.Domain.Workspaces.Workspaces.Events;

public sealed record WorkspaceCreatedDomainEvent : WorkspaceRootDomainEvent
{
    public Guid AccountId { get; }
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
        : base(workspaceId, occurredAt)
    {
        AccountId = accountId;
        Name = name;
        Slug = slug;
        CreatedBy = createdBy;
    }
}
