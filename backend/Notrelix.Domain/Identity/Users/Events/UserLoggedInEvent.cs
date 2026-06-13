using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Users.Events;

public sealed record UserLoggedInEvent(
    Guid UserId,
    DateTimeOffset LoggedInAt
) : DomainEvent(LoggedInAt, null, null);
