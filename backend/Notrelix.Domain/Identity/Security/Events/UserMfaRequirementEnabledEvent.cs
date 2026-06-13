using Notrelix.Domain.Common;
using Notrelix.Domain.Identity.Mfa;

namespace Notrelix.Domain.Identity.Security.Events;

public sealed record UserMfaRequirementEnabledEvent(
    Guid UserId,
    MfaMethodType PreferredMethod,
    DateTimeOffset EnabledAt
) : DomainEvent(EnabledAt, null, null);
