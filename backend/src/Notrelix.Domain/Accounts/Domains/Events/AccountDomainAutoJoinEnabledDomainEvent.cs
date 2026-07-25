namespace Notrelix.Domain.Accounts.Domains.Events;

[EventName("accounts.domain-auto-join-enabled")]
public sealed record AccountDomainAutoJoinEnabledDomainEvent(
    Guid AccountId,
    Guid DomainId,
    string Domain,
    DateTimeOffset OccurredAt
) : AccountScopedDomainEvent(AccountId, OccurredAt);
