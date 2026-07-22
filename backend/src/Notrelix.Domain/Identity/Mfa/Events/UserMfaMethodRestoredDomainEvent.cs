namespace Notrelix.Domain.Identity.Mfa.Events;

public sealed record UserMfaMethodRestoredDomainEvent(
    Guid MfaMethodId,
    Guid UserId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
