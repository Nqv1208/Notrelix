namespace Notrelix.Domain.Identity.Users.Events;

public sealed record UserSuspendedDomainEvent(
    Guid UserId,
    UserStatus PreviousStatus,
    Guid SuspendedBy,
    DateTimeOffset SuspendedAt,
    string? Reason
) : DomainEvent(SuspendedAt, null, null);
