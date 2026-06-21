using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Users.Events;

public sealed record UserPasswordChangedDomainEvent(
    Guid UserId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, null, null);
