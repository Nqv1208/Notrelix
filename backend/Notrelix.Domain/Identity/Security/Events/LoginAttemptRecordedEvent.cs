using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Security.Events;

public sealed record LoginAttemptRecordedEvent(
    Guid LoginAttemptId,
    Guid? UserId,
    string? AttemptedEmail,
    bool Succeeded,
    string? FailureReason,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, null, null);
