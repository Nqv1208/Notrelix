namespace Notrelix.Domain.Governance.Roles.Events;

[EventName("governance.custom-role-updated")]
public sealed record CustomRoleUpdatedDomainEvent(
    Guid AccountId,
    Guid RoleId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : AccountScopedDomainEvent(AccountId, OccurredAt);
