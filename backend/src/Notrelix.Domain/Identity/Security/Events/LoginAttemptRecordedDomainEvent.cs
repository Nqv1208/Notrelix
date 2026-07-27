namespace Notrelix.Domain.Identity.Security.Events;

[EventName("identity.login-attempt-recorded")]
public sealed record LoginAttemptRecordedDomainEvent(
    Guid LoginAttemptId,
    Guid? UserId,
    string? AttemptedEmail,
    bool Succeeded,
    string? FailureReason,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
