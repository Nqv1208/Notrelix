namespace Notrelix.Domain.Identity.Mfa.Events;

public sealed record UserMfaMethodAddedDomainEvent(
    Guid MfaMethodId,
    Guid UserId,
    MfaMethodType Type,
    DateTimeOffset AddedAt
) : GlobalDomainEvent(AddedAt);
