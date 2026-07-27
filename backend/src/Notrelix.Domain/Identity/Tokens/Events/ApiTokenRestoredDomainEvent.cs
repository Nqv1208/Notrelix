namespace Notrelix.Domain.Identity.Tokens.Events;

[EventName("identity.api-token-restored")]
public sealed record ApiTokenRestoredDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid TokenId { get; }

    public ApiTokenRestoredDomainEvent(
        Guid accountId,
        Guid workspaceId,
        Guid tokenId,
        DateTimeOffset occurredAt)
        : base(accountId, workspaceId, occurredAt)
    {
        TokenId = tokenId;
    }
}
