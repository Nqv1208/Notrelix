using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Users.Events;

public sealed record UserRegisteredDomainEvent(
    Guid UserId,
    Email Email,
    DateTimeOffset RegisteredAt
) : DomainEvent(RegisteredAt, null, null);
