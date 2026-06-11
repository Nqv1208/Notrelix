using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Security.Events;

public sealed record UserMfaRequirementEnabledEvent(
    Guid UserId,
    MfaMethodType PreferredMethod,
    DateTimeOffset EnabledAt
) : DomainEvent(EnabledAt);
