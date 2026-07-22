namespace Notrelix.Domain.Identity.Security.Events;

public sealed record UserSecuritySettingsCreatedDomainEvent(
    Guid UserSecuritySettingsId,
    Guid UserId,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
