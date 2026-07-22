namespace Notrelix.Domain.Accounts.Accounts.Events;

public sealed record AccountArchivedDomainEvent(
    Guid AccountId,
    Guid ArchivedBy,
    DateTimeOffset OccurredAt
) : AccountRootDomainEvent(AccountId, OccurredAt);
