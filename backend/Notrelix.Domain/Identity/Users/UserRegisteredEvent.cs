using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Identity.Users;

public sealed record UserRegisteredEvent(
    Guid WorkspaceId,
    Guid UserId,
    string Email,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
