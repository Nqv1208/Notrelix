using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Identity.Security;

public sealed record LoginAttemptRecordedEvent(
    Guid WorkspaceId,
    Guid UserId,
    string? Email,
    bool Succeeded,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
