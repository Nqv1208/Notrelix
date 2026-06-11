using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Identity.Users;

public sealed record UserLoggedInEvent(
    Guid WorkspaceId,
    Guid UserId,
    DateTimeOffset LoggedInAt,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
