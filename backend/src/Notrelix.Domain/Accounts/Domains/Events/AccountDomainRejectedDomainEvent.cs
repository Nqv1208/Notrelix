namespace Notrelix.Domain.Accounts.Domains.Events;

[EventName("accounts.domain-rejected")]
public sealed record AccountDomainRejectedDomainEvent(
    Guid AccountId,
    Guid DomainId,
    string Domain,
    DateTimeOffset OccurredAt
) : AccountScopedDomainEvent(AccountId, OccurredAt);
