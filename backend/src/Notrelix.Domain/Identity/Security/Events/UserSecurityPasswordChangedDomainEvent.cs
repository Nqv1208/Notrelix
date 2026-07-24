namespace Notrelix.Domain.Identity.Security.Events;

[EventName("identity.user-security-password-changed")]
public sealed record UserSecurityPasswordChangedDomainEvent(
    Guid UserId,
    DateTimeOffset ChangedAt
) : GlobalDomainEvent(ChangedAt);
