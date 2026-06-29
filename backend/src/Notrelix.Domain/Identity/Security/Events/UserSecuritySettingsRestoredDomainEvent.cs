namespace Notrelix.Domain.Identity.Security.Events;

public sealed record UserSecuritySettingsRestoredDomainEvent(
    Guid UserSecuritySettingsId,
    Guid UserId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt, RestoredBy);
