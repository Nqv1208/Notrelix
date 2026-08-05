namespace Notrelix.Domain.Accounts.Domains.Events;

[EventName("accounts.domain-verified")]
public sealed record AccountDomainVerifiedDomainEvent(
    Guid AccountId,
    Guid DomainId,
    string Domain,
    DateTimeOffset OccurredAt
) : AccountScopedDomainEvent(AccountId, OccurredAt);
