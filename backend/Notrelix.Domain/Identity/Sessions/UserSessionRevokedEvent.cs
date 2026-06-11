using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Identity.Sessions;

public sealed record UserSessionRevokedEvent(
    Guid SessionId,
    Guid UserId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
