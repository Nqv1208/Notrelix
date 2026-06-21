using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Sessions.Events;

public sealed record UserSessionRefreshTokenRotatedDomainEvent(
    Guid SessionId,
    Guid UserId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, workspaceId: null, UserId);
