namespace Notrelix.Domain.Identity.Security.Events;

public sealed record UserSecuritySettingsUpdatedDomainEvent(
    Guid UserId,
    DateTimeOffset UpdatedAt
) : GlobalDomainEvent(UpdatedAt);
