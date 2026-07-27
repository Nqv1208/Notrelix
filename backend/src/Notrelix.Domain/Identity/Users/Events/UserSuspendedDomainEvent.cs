namespace Notrelix.Domain.Identity.Users.Events;

[EventName("identity.user-suspended")]
public sealed record UserSuspendedDomainEvent(
    Guid UserId,
    UserStatus PreviousStatus,
    Guid SuspendedBy,
    DateTimeOffset SuspendedAt,
    string? Reason
) : GlobalDomainEvent(SuspendedAt);
