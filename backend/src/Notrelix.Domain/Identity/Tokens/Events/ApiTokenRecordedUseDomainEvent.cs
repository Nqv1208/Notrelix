namespace Notrelix.Domain.Identity.Tokens.Events;

[EventName("identity.api-token-recorded-use")]
public sealed record ApiTokenRecordedUseDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid TokenId { get; }

    public ApiTokenRecordedUseDomainEvent(
        Guid accountId,
        Guid workspaceId,
        Guid tokenId,
        DateTimeOffset occurredAt)
        : base(accountId, workspaceId, occurredAt)
    {
        TokenId = tokenId;
    }
}
