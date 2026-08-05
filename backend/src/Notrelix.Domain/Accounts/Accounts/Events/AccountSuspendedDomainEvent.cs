namespace Notrelix.Domain.Accounts.Accounts.Events;

[EventName("accounts.account-suspended")]
public sealed record AccountSuspendedDomainEvent(
    Guid AccountId,
    AccountStatus PreviousStatus,
    Guid SuspendedBy,
    DateTimeOffset OccurredAt,
    string? Reason
) : AccountScopedDomainEvent(AccountId, OccurredAt);
