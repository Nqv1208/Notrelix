using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Users.Events;

public sealed record UserRegisteredEvent(
    Guid UserId,
    Email Email,
    DateTimeOffset RegisteredAt
) : DomainEvent(RegisteredAt, null, null);
