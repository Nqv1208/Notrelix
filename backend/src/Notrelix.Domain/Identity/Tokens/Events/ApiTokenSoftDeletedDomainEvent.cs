namespace Notrelix.Domain.Identity.Tokens.Events;

public record ApiTokenSoftDeletedDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid AccountId { get; }
    public Guid TokenId { get; }

    public ApiTokenSoftDeletedDomainEvent(
        Guid accountId,
        Guid workspaceId,
        Guid tokenId,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(accountId, workspaceId, occurredAt, actorUserId)
    {
        AccountId = accountId;
        TokenId = tokenId;
    }
}
