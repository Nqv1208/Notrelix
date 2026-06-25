namespace Notrelix.Domain.Identity.Tokens.Events;

public record ApiTokenRestoredDomainEvent : DomainEvent
{
    public Guid TokenId { get; }

    public ApiTokenRestoredDomainEvent(
        Guid workspaceId,
        Guid tokenId,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(occurredAt, workspaceId, actorUserId)
    {
        TokenId = tokenId;
    }
}
