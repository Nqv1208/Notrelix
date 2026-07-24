namespace Notrelix.Domain.Identity.Users.Events;

[EventName("identity.user-password-changed")]
public sealed record UserPasswordChangedDomainEvent(
    Guid UserId,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
