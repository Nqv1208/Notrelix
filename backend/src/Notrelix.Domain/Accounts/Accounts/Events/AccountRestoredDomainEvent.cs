namespace Notrelix.Domain.Accounts.Accounts.Events;

public sealed record AccountRestoredDomainEvent(
    Guid AccountId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : AccountRootDomainEvent(AccountId, OccurredAt);
