namespace Notrelix.Domain.Identity.Mfa.Events;

public sealed record UserMfaMethodDisabledDomainEvent(
    Guid MfaMethodId,
    Guid UserId,
    MfaMethodType Type,
    DateTimeOffset DisabledAt
) : GlobalDomainEvent(DisabledAt);
