namespace Notrelix.Domain.Identity.Tokens.Events;

public record ApiTokenRevokedDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid TokenId { get; }

    public ApiTokenRevokedDomainEvent(
        Guid workspaceId,
        Guid tokenId,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(workspaceId, occurredAt, actorUserId)
    {
        TokenId = tokenId;
    }
}
