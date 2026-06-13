using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Security.Events;

public sealed record PasswordChangeRequiredEvent(
    Guid UserId,
    DateTimeOffset RequiredAt
) : DomainEvent(RequiredAt, null, null);
