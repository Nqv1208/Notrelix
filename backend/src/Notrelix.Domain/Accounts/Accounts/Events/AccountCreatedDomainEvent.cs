namespace Notrelix.Domain.Accounts.Accounts.Events;

public sealed record AccountCreatedDomainEvent(
    Guid AccountId,
    string Name,
    string Slug,
    AccountType Type,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : AccountRootDomainEvent(AccountId, OccurredAt, CreatedBy);
