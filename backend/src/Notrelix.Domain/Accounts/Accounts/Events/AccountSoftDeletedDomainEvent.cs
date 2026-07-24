namespace Notrelix.Domain.Accounts.Accounts.Events;

[EventName("accounts.account-soft-deleted")]
public sealed record AccountSoftDeletedDomainEvent(
    Guid AccountId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt,
    string? Reason
) : AccountScopedDomainEvent(AccountId, OccurredAt);
