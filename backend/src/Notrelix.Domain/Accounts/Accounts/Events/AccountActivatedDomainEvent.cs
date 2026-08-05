namespace Notrelix.Domain.Accounts.Accounts.Events;

[EventName("accounts.account-activated")]
public sealed record AccountActivatedDomainEvent(
    Guid AccountId,
    AccountStatus PreviousStatus,
    Guid ActivatedBy,
    DateTimeOffset OccurredAt
) : AccountScopedDomainEvent(AccountId, OccurredAt);
