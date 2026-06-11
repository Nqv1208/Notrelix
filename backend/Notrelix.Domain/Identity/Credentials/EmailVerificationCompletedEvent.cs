using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Identity.Credentials;

public sealed record EmailVerificationCompletedEvent(
    Guid WorkspaceId,
    Guid UserId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
