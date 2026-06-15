using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Tokens;

public record ApiTokenSoftDeletedDomainEvent : DomainEvent
{
    public Guid TokenId { get; }

    public ApiTokenSoftDeletedDomainEvent(
        Guid workspaceId,
        Guid tokenId,
        Guid? actorUserId,
        DateTimeOffset occurredAt) 
        : base(occurredAt, workspaceId, actorUserId)
    {
        TokenId = tokenId;
    }
}
