namespace Notrelix.Domain.Identity.Users.Events;

public sealed record UserDeactivatedDomainEvent(
    Guid UserId,
    UserStatus PreviousStatus,
    Guid DeactivatedBy,
    DateTimeOffset DeactivatedAt,
    string? Reason
) : GlobalDomainEvent(DeactivatedAt);
