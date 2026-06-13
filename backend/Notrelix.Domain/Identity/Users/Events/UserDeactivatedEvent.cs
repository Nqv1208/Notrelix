using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Users.Events;

public sealed record UserDeactivatedEvent(
    Guid UserId,
    UserStatus PreviousStatus,
    Guid DeactivatedBy,
    DateTimeOffset DeactivatedAt,
    string? Reason
) : DomainEvent(DeactivatedAt, null, null);
