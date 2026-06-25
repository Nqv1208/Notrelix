namespace Notrelix.Domain.Identity.Security.Events;

public sealed record PasswordChangeRequiredDomainEvent(
    Guid UserId,
    DateTimeOffset RequiredAt
) : DomainEvent(RequiredAt, null, null);
