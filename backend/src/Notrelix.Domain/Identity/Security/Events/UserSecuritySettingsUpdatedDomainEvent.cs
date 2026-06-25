namespace Notrelix.Domain.Identity.Security.Events;

public sealed record UserSecuritySettingsUpdatedDomainEvent(
    Guid UserId,
    DateTimeOffset UpdatedAt
) : DomainEvent(UpdatedAt, null, null);
