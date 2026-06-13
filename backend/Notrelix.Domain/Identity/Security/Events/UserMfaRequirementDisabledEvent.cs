using Notrelix.Domain.Common;
using Notrelix.Domain.Identity.Mfa;

namespace Notrelix.Domain.Identity.Security.Events;

public sealed record UserMfaRequirementDisabledEvent(
    Guid UserId,
    MfaMethodType? PreviousMethod,
    DateTimeOffset DisabledAt
) : DomainEvent(DisabledAt);
