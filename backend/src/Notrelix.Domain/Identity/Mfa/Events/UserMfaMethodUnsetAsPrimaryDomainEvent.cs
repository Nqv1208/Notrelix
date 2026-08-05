namespace Notrelix.Domain.Identity.Mfa.Events;

[EventName("identity.user-mfa-method-unset-as-primary")]
public sealed record UserMfaMethodUnsetAsPrimaryDomainEvent(
    Guid MfaMethodId,
    Guid UserId,
    MfaMethodType Type,
    DateTimeOffset UpdatedAt
) : GlobalDomainEvent(UpdatedAt);
