namespace Notrelix.Domain.Identity.Tokens.Events;

public record ApiTokenRevokedDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid AccountId { get; }
    public Guid TokenId { get; }

    public ApiTokenRevokedDomainEvent(
        Guid accountId,
        Guid workspaceId,
        Guid tokenId,
        DateTimeOffset occurredAt)
        : base(accountId, workspaceId, occurredAt)
    {
        AccountId = accountId;
        TokenId = tokenId;
    }
}
