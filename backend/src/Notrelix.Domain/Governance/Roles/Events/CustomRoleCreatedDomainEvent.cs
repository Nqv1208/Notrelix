namespace Notrelix.Domain.Governance.Roles.Events;

[EventName("governance.custom-role-created")]
public sealed record CustomRoleCreatedDomainEvent(
    Guid AccountId,
    Guid RoleId,
    string Name,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : AccountScopedDomainEvent(AccountId, OccurredAt);
