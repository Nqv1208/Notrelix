namespace Notrelix.Domain.Identity.Mfa.Events;

[EventName("identity.user-mfa-method-disabled")]
public sealed record UserMfaMethodDisabledDomainEvent(
    Guid MfaMethodId,
    Guid UserId,
    MfaMethodType Type,
    DateTimeOffset DisabledAt
) : GlobalDomainEvent(DisabledAt);
