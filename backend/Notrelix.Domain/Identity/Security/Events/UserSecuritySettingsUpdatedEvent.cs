using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Security.Events;

public sealed record UserSecuritySettingsUpdatedEvent(
    Guid UserId,
    DateTimeOffset UpdatedAt
) : DomainEvent(UpdatedAt);
