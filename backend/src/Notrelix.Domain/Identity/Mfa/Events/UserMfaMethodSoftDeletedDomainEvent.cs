namespace Notrelix.Domain.Identity.Mfa.Events;

[EventName("identity.user-mfa-method-soft-deleted")]
public sealed record UserMfaMethodSoftDeletedDomainEvent(
    Guid MfaMethodId,
    Guid UserId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt,
    string? Reason
) : GlobalDomainEvent(OccurredAt);
