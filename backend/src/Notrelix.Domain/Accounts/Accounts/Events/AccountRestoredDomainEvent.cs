namespace Notrelix.Domain.Accounts.Accounts.Events;

[EventName("accounts.account-restored")]
public sealed record AccountRestoredDomainEvent(
    Guid AccountId,
    AccountStatus Status,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : AccountScopedDomainEvent(AccountId, OccurredAt);
