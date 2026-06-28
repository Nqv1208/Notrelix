namespace Notrelix.Domain.Identity.Security.Events;

public sealed record UserSecurityPasswordChangedDomainEvent(
    Guid UserId,
    DateTimeOffset ChangedAt
) : GlobalDomainEvent(ChangedAt);
