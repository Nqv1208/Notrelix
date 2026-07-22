namespace Notrelix.Domain.Accounts.Accounts.Events;

public sealed record AccountSuspendedDomainEvent(
    Guid AccountId,
    AccountStatus PreviousStatus,
    Guid SuspendedBy,
    DateTimeOffset OccurredAt,
    string? Reason
) : AccountRootDomainEvent(AccountId, OccurredAt);
