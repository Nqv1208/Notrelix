namespace Notrelix.Domain.Accounts.Accounts.Events;

public sealed record AccountRenamedDomainEvent(
    Guid AccountId,
    string OldName,
    string NewName,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : AccountRootDomainEvent(AccountId, OccurredAt, UpdatedBy);
