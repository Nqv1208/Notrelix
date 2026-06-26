namespace Notrelix.Domain.Identity.Tokens.Events;

public record ApiTokenRevokedDomainEvent : DomainEvent
{
    public Guid TokenId { get; }

    public ApiTokenRevokedDomainEvent(
        Guid workspaceId,
        Guid tokenId,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(occurredAt, workspaceId, actorUserId)
    {
        TokenId = tokenId;
    }
}
