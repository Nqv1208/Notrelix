using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Identity.Security;

public sealed record MfaEnabledEvent(
    Guid WorkspaceId,
    Guid UserId,
    MfaMethodType Type,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
