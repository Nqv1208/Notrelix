namespace Notrelix.Domain.Identity.Mfa.Events;

public sealed record UserMfaMethodVerifiedDomainEvent(
    Guid MfaMethodId,
    Guid UserId,
    MfaMethodType Type,
    DateTimeOffset VerifiedAt
) : DomainEvent(VerifiedAt, null, null);
