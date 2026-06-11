using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Mfa.Events;

public sealed record UserMfaMethodAddedEvent(
    Guid MfaMethodId,
    Guid UserId,
    MfaMethodType Type,
    DateTimeOffset AddedAt
) : DomainEvent(AddedAt);
