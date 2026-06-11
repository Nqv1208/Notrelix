using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Security.Events;

public sealed record UserMfaRequirementDisabledEvent(
    Guid UserId,
    MfaMethodType? PreviousMethod,
    DateTimeOffset DisabledAt
) : DomainEvent(DisabledAt);
