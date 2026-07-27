namespace Notrelix.Domain.Identity.Tokens.Events;

[EventName("identity.api-token-soft-deleted")]
public sealed record ApiTokenSoftDeletedDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid TokenId { get; }

    public ApiTokenSoftDeletedDomainEvent(
        Guid accountId,
        Guid workspaceId,
        Guid tokenId,
        DateTimeOffset occurredAt)
        : base(accountId, workspaceId, occurredAt)
    {
        TokenId = tokenId;
    }
}
