using Notrelix.Domain.Common;
using Notrelix.Domain.Identity.Mfa;

namespace Notrelix.Domain.Identity.Security.Events;

public sealed record UserMfaRequirementDisabledDomainEvent(
    Guid UserId,
    MfaMethodType? PreviousMethod,
    DateTimeOffset DisabledAt
) : DomainEvent(DisabledAt, null, null);
