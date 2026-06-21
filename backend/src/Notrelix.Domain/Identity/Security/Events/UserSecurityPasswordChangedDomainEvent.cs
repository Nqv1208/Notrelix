using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Security.Events;

public sealed record UserSecurityPasswordChangedDomainEvent(
    Guid UserId,
    DateTimeOffset ChangedAt
) : DomainEvent(ChangedAt, null, null);
