namespace Notrelix.Domain.Accounts.Accounts.Events;

[EventName("accounts.account-created")]
public sealed record AccountCreatedDomainEvent(
    Guid AccountId,
    string Name,
    string Slug,
    AccountType Type,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : AccountScopedDomainEvent(AccountId, OccurredAt);
