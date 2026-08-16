namespace Notrelix.Domain.Identity.Mfa.Events;

[EventName("identity.user-mfa-recovery-batch-created")]
public sealed record MfaRecoveryBatchCreatedDomainEvent(
    Guid BatchId,
    Guid UserId,
    int CodeCount,
    DateTimeOffset CreatedAt
) : GlobalDomainEvent(CreatedAt);
