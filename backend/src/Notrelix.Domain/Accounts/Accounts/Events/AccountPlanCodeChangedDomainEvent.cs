namespace Notrelix.Domain.Accounts.Accounts.Events;

[EventName("accounts.account-plan-code-changed")]
public sealed record AccountPlanCodeChangedDomainEvent(
    Guid AccountId,
    string? OldPlanCode,
    string? NewPlanCode,
    Guid ChangedBy,
    DateTimeOffset OccurredAt
) : AccountScopedDomainEvent(AccountId, OccurredAt);
