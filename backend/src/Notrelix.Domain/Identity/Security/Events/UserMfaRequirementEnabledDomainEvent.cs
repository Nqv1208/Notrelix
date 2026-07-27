using Notrelix.Domain.Identity.Mfa;

namespace Notrelix.Domain.Identity.Security.Events;

[EventName("identity.user-mfa-requirement-enabled")]
public sealed record UserMfaRequirementEnabledDomainEvent(
    Guid UserId,
    MfaMethodType PreferredMethod,
    DateTimeOffset EnabledAt
) : GlobalDomainEvent(EnabledAt);
