namespace Notrelix.Domain.Identity.Security.Events;

[EventName("identity.user-security-settings-soft-deleted")]
public sealed record UserSecuritySettingsSoftDeletedDomainEvent(
    Guid UserSecuritySettingsId,
    Guid UserId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt,
    string? Reason
) : GlobalDomainEvent(OccurredAt);
