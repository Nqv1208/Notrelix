namespace Notrelix.Domain.Identity.Tokens.Events;

public record ApiTokenCreatedDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid AccountId { get; }
    public Guid TokenId { get; }
    public string Name { get; }

    public ApiTokenCreatedDomainEvent(
        Guid accountId,
        Guid workspaceId,
        Guid tokenId,
        string name,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(accountId, workspaceId, occurredAt, actorUserId)
    {
        AccountId = accountId;
        TokenId = tokenId;
        Name = name;
    }
}
