using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Identity.Profiles;

public sealed record UserProfileUpdatedEvent(
    Guid WorkspaceId,
    Guid UserId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
