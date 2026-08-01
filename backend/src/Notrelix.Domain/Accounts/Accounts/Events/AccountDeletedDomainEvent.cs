namespace Notrelix.Domain.Accounts.Accounts.Events;

[EventName("accounts.account-deleted")]
public sealed record AccountDeletedDomainEvent(
    Guid AccountId,
    AccountStatus Status,
    Guid DeletedBy,
    DateTimeOffset OccurredAt,
    string? Reason
) : AccountScopedDomainEvent(AccountId, OccurredAt);
