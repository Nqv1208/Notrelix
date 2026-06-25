namespace Notrelix.Domain.Identity.Users.Events;

public sealed record UserDeactivatedDomainEvent(
    Guid UserId,
    UserStatus PreviousStatus,
    Guid DeactivatedBy,
    DateTimeOffset DeactivatedAt,
    string? Reason
) : DomainEvent(DeactivatedAt, null, null);
