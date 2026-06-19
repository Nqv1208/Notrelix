using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Sessions.Events;

public sealed record UserSessionCreatedDomainEvent(
    Guid SessionId,
    Guid UserId,
    DateTimeOffset CreatedAt
) : DomainEvent(CreatedAt, null, null);
