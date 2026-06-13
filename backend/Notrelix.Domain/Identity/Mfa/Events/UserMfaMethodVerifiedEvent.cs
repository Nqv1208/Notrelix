using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Mfa.Events;

public sealed record UserMfaMethodVerifiedEvent(
    Guid MfaMethodId,
    Guid UserId,
    MfaMethodType Type,
    DateTimeOffset VerifiedAt
) : DomainEvent(VerifiedAt);
