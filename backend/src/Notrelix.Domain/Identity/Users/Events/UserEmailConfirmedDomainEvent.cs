using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Users.Events;

public sealed record UserEmailConfirmedDomainEvent(
    Guid UserId,
    string Email,
    DateTimeOffset ConfirmedAt
) : GlobalDomainEvent(ConfirmedAt);
