namespace Notrelix.Domain.Identity.Security.Events;

[EventName("identity.user-security-settings-updated")]
public sealed record UserSecuritySettingsUpdatedDomainEvent(
    Guid UserId,
    DateTimeOffset UpdatedAt
) : GlobalDomainEvent(UpdatedAt);
