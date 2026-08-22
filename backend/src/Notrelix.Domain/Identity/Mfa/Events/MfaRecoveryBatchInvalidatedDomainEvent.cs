namespace Notrelix.Domain.Identity.Mfa.Events;

[EventName("identity.user-mfa-recovery-batch-invalidated")]
public sealed record MfaRecoveryBatchInvalidatedDomainEvent(
    Guid BatchId,
    Guid UserId,
    DateTimeOffset InvalidatedAt
) : GlobalDomainEvent(InvalidatedAt);
