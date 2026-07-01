namespace Notrelix.Domain.Identity.Tokens.Events;

public record ApiTokenRevokedDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid AccountId { get; }
    public Guid TokenId { get; }

    public ApiTokenRevokedDomainEvent(
        Guid accountId,
        Guid workspaceId,
        Guid tokenId,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(workspaceId, occurredAt, actorUserId)
    {
        AccountId = accountId;
        TokenId = tokenId;
    }
}
