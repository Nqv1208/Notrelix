namespace Notrelix.Domain.Identity.Users.Events;

[EventName("identity.user-deactivated")]
public sealed record UserDeactivatedDomainEvent(
    Guid UserId,
    UserStatus PreviousStatus,
    Guid DeactivatedBy,
    DateTimeOffset DeactivatedAt,
    string? Reason
) : GlobalDomainEvent(DeactivatedAt);
