namespace Notrelix.Domain.Accounts.Accounts.Events;

public sealed record AccountSoftDeletedDomainEvent(
    Guid AccountId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt,
    string? Reason
) : AccountRootDomainEvent(AccountId, OccurredAt);
