namespace Notrelix.Domain.Accounts.Accounts.Events;

[EventName("accounts.account-archived")]
public sealed record AccountArchivedDomainEvent(
    Guid AccountId,
    Guid ArchivedBy,
    DateTimeOffset OccurredAt
) : AccountScopedDomainEvent(AccountId, OccurredAt);
