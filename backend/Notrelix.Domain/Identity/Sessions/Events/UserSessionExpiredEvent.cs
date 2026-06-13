using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Sessions.Events;

public sealed record UserSessionExpiredEvent(
    Guid SessionId,
    Guid UserId,
    DateTimeOffset ExpiredAt
) : DomainEvent(ExpiredAt);
