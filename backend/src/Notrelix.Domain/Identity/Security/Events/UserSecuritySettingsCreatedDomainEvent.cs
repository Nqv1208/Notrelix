namespace Notrelix.Domain.Identity.Security.Events;

[EventName("identity.user-security-settings-created")]
public sealed record UserSecuritySettingsCreatedDomainEvent(
    Guid UserSecuritySettingsId,
    Guid UserId,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
