namespace Notrelix.Domain.Accounts.Accounts.Events;

[EventName("accounts.account-renamed")]
public sealed record AccountRenamedDomainEvent(
    Guid AccountId,
    string OldName,
    string NewName,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : AccountScopedDomainEvent(AccountId, OccurredAt);
