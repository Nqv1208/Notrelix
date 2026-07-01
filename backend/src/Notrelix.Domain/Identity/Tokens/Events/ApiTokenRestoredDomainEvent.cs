namespace Notrelix.Domain.Identity.Tokens.Events;

public record ApiTokenRestoredDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid AccountId { get; }
    public Guid TokenId { get; }

    public ApiTokenRestoredDomainEvent(
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
