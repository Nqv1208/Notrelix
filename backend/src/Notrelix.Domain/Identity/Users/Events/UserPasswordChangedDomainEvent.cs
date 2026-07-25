namespace Notrelix.Domain.Identity.Users.Events;

[EventName("identity.user-password-changed")]
public sealed record UserPasswordChangedDomainEvent(
    Guid UserId,
    Guid ChangedBy,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);