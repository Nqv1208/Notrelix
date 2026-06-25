namespace Notrelix.Domain.Identity.Mfa.Events;

public sealed record UserMfaMethodUnsetAsPrimaryDomainEvent(
    Guid MfaMethodId,
    Guid UserId,
    MfaMethodType Type,
    DateTimeOffset UpdatedAt
) : DomainEvent(UpdatedAt, null, null);
