namespace Notrelix.Domain.Identity.Tokens.Events;

public record ApiTokenRestoredDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid TokenId { get; }

    public ApiTokenRestoredDomainEvent(
        Guid workspaceId,
        Guid tokenId,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(workspaceId, occurredAt, actorUserId)
    {
        TokenId = tokenId;
    }
}
