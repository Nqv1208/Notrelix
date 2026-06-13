using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Mfa.Events;

public sealed record UserMfaMethodDisabledEvent(
    Guid MfaMethodId,
    Guid UserId,
    MfaMethodType Type,
    DateTimeOffset DisabledAt
) : DomainEvent(DisabledAt, null, null);
