namespace Notrelix.Domain.Identity.Mfa.Events;

[EventName("identity.user-mfa-method-set-as-primary")]
public sealed record UserMfaMethodSetAsPrimaryDomainEvent(
    Guid MfaMethodId,
    Guid UserId,
    MfaMethodType Type,
    DateTimeOffset UpdatedAt
) : GlobalDomainEvent(UpdatedAt);
