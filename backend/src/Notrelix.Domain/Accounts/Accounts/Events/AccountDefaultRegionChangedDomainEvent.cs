namespace Notrelix.Domain.Accounts.Accounts.Events;

[EventName("accounts.account-default-region-changed")]
public sealed record AccountDefaultRegionChangedDomainEvent(
    Guid AccountId,
    string? OldRegionCode,
    string? NewRegionCode,
    Guid ChangedBy,
    DateTimeOffset OccurredAt
) : AccountScopedDomainEvent(AccountId, OccurredAt);
