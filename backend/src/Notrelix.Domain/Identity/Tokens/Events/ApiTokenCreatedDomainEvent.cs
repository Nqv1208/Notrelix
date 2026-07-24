namespace Notrelix.Domain.Identity.Tokens.Events;

[EventName("identity.api-token-created")]
public sealed record ApiTokenCreatedDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid TokenId { get; }
    public string Name { get; }

    public ApiTokenCreatedDomainEvent(
        Guid accountId,
        Guid workspaceId,
        Guid tokenId,
        string name,
        DateTimeOffset occurredAt)
        : base(accountId, workspaceId, occurredAt)
    {
        TokenId = tokenId;
        Name = name;
    }
}
