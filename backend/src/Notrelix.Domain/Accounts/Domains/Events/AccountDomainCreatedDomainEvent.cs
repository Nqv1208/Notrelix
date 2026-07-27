namespace Notrelix.Domain.Accounts.Domains.Events;

[EventName("accounts.domain-created")]
public sealed record AccountDomainCreatedDomainEvent(
    Guid AccountId,
    Guid DomainId,
    string Domain,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : AccountScopedDomainEvent(AccountId, OccurredAt);