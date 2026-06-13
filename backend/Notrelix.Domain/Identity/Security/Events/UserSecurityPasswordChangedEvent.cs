using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Security.Events;

public sealed record UserSecurityPasswordChangedEvent(
    Guid UserId,
    DateTimeOffset ChangedAt
) : DomainEvent(ChangedAt);
