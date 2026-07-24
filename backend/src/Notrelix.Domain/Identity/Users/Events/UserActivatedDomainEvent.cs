namespace Notrelix.Domain.Identity.Users.Events;

[EventName("identity.user-activated")]
public sealed record UserActivatedDomainEvent(
    Guid UserId,
    UserStatus PreviousStatus,
    Guid ActivatedBy,
    DateTimeOffset ActivatedAt,
    string? Reason
) : GlobalDomainEvent(ActivatedAt);
