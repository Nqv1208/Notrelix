using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Security.Events;

public sealed record UserSecuritySettingsCreatedDomainEvent(
    Guid UserSecuritySettingsId,
    Guid UserId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, workspaceId: null, UserId);
