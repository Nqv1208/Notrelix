namespace Notrelix.Domain.Governance.Roles.Events;

[EventName("governance.custom-role-archived")]
public sealed record CustomRoleArchivedDomainEvent(
    Guid AccountId,
    Guid RoleId,
    Guid ArchivedBy,
    DateTimeOffset OccurredAt
) : AccountScopedDomainEvent(AccountId, OccurredAt);
