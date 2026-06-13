using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Sessions.Events;

public sealed record UserSessionRevokedEvent(
    Guid SessionId,
    Guid UserId,
    DateTimeOffset RevokedAt,
    string? Reason
) : DomainEvent(RevokedAt);
