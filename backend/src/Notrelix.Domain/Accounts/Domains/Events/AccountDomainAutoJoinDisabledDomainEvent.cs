namespace Notrelix.Domain.Accounts.Domains.Events;

[EventName("accounts.domain-auto-join-disabled")]
public sealed record AccountDomainAutoJoinDisabledDomainEvent(
    Guid AccountId,
    Guid DomainId,
    string Domain,
    DateTimeOffset OccurredAt
) : AccountScopedDomainEvent(AccountId, OccurredAt);
