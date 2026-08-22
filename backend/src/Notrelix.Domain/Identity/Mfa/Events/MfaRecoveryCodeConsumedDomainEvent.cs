namespace Notrelix.Domain.Identity.Mfa.Events;

[EventName("identity.user-mfa-recovery-code-consumed")]
public sealed record MfaRecoveryCodeConsumedDomainEvent(
    Guid BatchId,
    Guid UserId,
    DateTimeOffset ConsumedAt
) : GlobalDomainEvent(ConsumedAt);
