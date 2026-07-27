namespace Notrelix.Domain.Identity.Mfa.Events;

[EventName("identity.user-mfa-method-verified")]
public sealed record UserMfaMethodVerifiedDomainEvent(
    Guid MfaMethodId,
    Guid UserId,
    MfaMethodType Type,
    DateTimeOffset VerifiedAt
) : GlobalDomainEvent(VerifiedAt);
