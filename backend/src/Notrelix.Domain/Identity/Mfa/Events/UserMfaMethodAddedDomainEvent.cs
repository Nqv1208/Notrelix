namespace Notrelix.Domain.Identity.Mfa.Events;

[EventName("identity.user-mfa-method-added")]
public sealed record UserMfaMethodAddedDomainEvent(
    Guid MfaMethodId,
    Guid UserId,
    MfaMethodType Type,
    DateTimeOffset AddedAt
) : GlobalDomainEvent(AddedAt);
