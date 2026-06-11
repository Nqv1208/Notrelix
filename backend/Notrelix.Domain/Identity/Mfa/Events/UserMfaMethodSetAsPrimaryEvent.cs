using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Mfa.Events;

public sealed record UserMfaMethodSetAsPrimaryEvent(
    Guid MfaMethodId,
    Guid UserId,
    MfaMethodType Type,
    DateTimeOffset UpdatedAt
) : DomainEvent(UpdatedAt);
