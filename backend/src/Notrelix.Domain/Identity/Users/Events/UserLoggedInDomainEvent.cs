using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Users.Events;

public sealed record UserLoggedInDomainEvent(
    Guid UserId,
    DateTimeOffset LoggedInAt
) : DomainEvent(LoggedInAt, null, null);
