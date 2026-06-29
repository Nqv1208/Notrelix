namespace Notrelix.Domain.Identity.Tokens.Events;

public record ApiTokenCreatedDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid TokenId { get; }
    public string Name { get; }

    public ApiTokenCreatedDomainEvent(
        Guid workspaceId,
        Guid tokenId,
        string name,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(workspaceId, occurredAt, actorUserId)
    {
        TokenId = tokenId;
        Name = name;
    }
}
