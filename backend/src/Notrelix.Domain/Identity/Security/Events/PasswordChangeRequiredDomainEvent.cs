namespace Notrelix.Domain.Identity.Security.Events;

[EventName("identity.password-change-required")]
public sealed record PasswordChangeRequiredDomainEvent(
    Guid UserId,
    DateTimeOffset RequiredAt
) : GlobalDomainEvent(RequiredAt);
