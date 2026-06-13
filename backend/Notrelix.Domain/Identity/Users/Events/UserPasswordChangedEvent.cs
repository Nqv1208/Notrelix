using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Users.Events;

public sealed record UserPasswordChangedEvent(
    Guid UserId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
