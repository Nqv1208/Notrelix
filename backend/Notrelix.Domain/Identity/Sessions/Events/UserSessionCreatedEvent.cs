using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Sessions.Events;

public sealed record UserSessionCreatedEvent(
    Guid SessionId,
    Guid UserId,
    DateTimeOffset CreatedAt
) : DomainEvent(CreatedAt);
