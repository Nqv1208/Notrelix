using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Security.Events;

public sealed record UserSecuritySettingsSoftDeletedDomainEvent(
    Guid UserSecuritySettingsId,
    Guid UserId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt,
    string? Reason
) : DomainEvent(OccurredAt, workspaceId: null, DeletedBy);
