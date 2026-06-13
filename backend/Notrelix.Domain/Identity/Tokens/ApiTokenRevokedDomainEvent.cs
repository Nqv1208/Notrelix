using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Tokens;

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
