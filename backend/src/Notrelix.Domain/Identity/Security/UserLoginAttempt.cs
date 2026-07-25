using Notrelix.Domain.Identity.Security.Events;

namespace Notrelix.Domain.Identity.Security;

public class UserLoginAttempt : AggregateRoot
{
    public Guid? UserId { get; private set; }
    public string? AttemptedEmail { get; private set; }
    public bool Succeeded { get; private set; }
    public string? FailureReason { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }

    private UserLoginAttempt() : base() { }

    public static UserLoginAttempt Record(
        Guid? userId,
        string? attemptedEmail,
        bool succeeded,
        DateTimeOffset occurredAt,
        string? failureReason = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        if (userId is null && string.IsNullOrWhiteSpace(attemptedEmail))
        {
            throw new BusinessRuleException(IdentityRuleCodes.Identity_LoginAttempt_MustHaveUserIdOrEmail, "Login attempt must have either user id or attempted email.");
        }

        if (succeeded && !string.IsNullOrWhiteSpace(failureReason))
        {
            throw new BusinessRuleException(IdentityRuleCodes.Identity_LoginAttempt_SuccessfulCannotHaveReason, "Successful login attempt cannot have failure reason.");
        }

        if (!succeeded && string.IsNullOrWhiteSpace(failureReason))
        {
            throw new BusinessRuleException(IdentityRuleCodes.Identity_LoginAttempt_FailedMustHaveReason, "Failed login attempt must have failure reason.");
        }

        var attempt = new UserLoginAttempt
        {
            UserId = userId,
            AttemptedEmail = attemptedEmail?.Trim().ToLowerInvariant(),
            Succeeded = succeeded,
            FailureReason = failureReason?.Trim(),
            IpAddress = ipAddress?.Trim(),
            UserAgent = userAgent?.Trim(),
            OccurredAt = occurredAt
        };

        attempt.RaiseDomainEvent(new LoginAttemptRecordedDomainEvent(
            attempt.Id,
            attempt.UserId,
            attempt.AttemptedEmail,
            attempt.Succeeded,
            attempt.FailureReason,
            attempt.OccurredAt));

        return attempt;
    }
}
