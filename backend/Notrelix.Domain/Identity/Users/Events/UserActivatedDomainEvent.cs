using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Users.Events;

public sealed record UserActivatedDomainEvent(
    Guid UserId,
    UserStatus PreviousStatus,
    Guid ActivatedBy,
    DateTimeOffset ActivatedAt,
    string? Reason
) : DomainEvent(ActivatedAt, null, null);
