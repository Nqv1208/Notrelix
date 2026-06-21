using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Security.Events;

public sealed record UserSecuritySettingsRestoredDomainEvent(
    Guid UserSecuritySettingsId,
    Guid UserId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, workspaceId: null, RestoredBy);
