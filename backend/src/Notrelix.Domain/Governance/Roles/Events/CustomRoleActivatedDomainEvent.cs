namespace Notrelix.Domain.Governance.Roles.Events;

[EventName("governance.custom-role-activated")]
public sealed record CustomRoleActivatedDomainEvent(
    Guid AccountId,
    Guid RoleId,
    Guid ActivatedBy,
    DateTimeOffset OccurredAt
) : AccountScopedDomainEvent(AccountId, OccurredAt);
