namespace Notrelix.Domain.Identity.Tokens.Events;

[EventName("identity.api-token-revoked")]
public sealed record ApiTokenRevokedDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid TokenId { get; }

    public ApiTokenRevokedDomainEvent(
        Guid accountId,
        Guid workspaceId,
        Guid tokenId,
        DateTimeOffset occurredAt)
        : base(accountId, workspaceId, occurredAt)
    {
        TokenId = tokenId;
    }
}
