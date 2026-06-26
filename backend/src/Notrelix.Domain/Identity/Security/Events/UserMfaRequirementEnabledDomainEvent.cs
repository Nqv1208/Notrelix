using Notrelix.Domain.Identity.Mfa;

namespace Notrelix.Domain.Identity.Security.Events;

public sealed record UserMfaRequirementEnabledDomainEvent(
    Guid UserId,
    MfaMethodType PreferredMethod,
    DateTimeOffset EnabledAt
) : DomainEvent(EnabledAt, null, null);
