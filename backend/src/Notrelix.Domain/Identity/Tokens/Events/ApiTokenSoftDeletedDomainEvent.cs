namespace Notrelix.Domain.Identity.Tokens.Events;

public record ApiTokenSoftDeletedDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid TokenId { get; }

    public ApiTokenSoftDeletedDomainEvent(
        Guid workspaceId,
        Guid tokenId,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(workspaceId, occurredAt, actorUserId)
    {
        TokenId = tokenId;
    }
}
