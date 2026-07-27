namespace Notrelix.Domain.Identity.Security.Events;

[EventName("identity.user-security-settings-restored")]
public sealed record UserSecuritySettingsRestoredDomainEvent(
    Guid UserSecuritySettingsId,
    Guid UserId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
