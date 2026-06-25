namespace Notrelix.Domain.Identity.Mfa.Events;

public sealed record UserMfaMethodSetAsPrimaryDomainEvent(
    Guid MfaMethodId,
    Guid UserId,
    MfaMethodType Type,
    DateTimeOffset UpdatedAt
) : DomainEvent(UpdatedAt, null, null);
