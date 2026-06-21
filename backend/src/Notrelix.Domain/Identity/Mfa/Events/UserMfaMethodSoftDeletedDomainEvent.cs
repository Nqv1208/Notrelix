using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Mfa.Events;

public sealed record UserMfaMethodSoftDeletedDomainEvent(
    Guid MfaMethodId,
    Guid UserId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt,
    string? Reason
) : DomainEvent(OccurredAt, workspaceId: null, DeletedBy);
