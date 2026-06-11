using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Identity.Users;

public sealed record UserLoggedInEvent(
    Guid UserId,
    DateTimeOffset LoggedInAt,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
