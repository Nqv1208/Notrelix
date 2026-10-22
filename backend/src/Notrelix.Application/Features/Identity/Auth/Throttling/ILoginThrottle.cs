using Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.Identity.Auth.Throttling;

/// <summary>The anonymous credential step a counter protects. Counters never mix across scopes.</summary>
public enum LoginThrottleScope
{
    /// <summary>Email and password step. The subject is the submitted email, whether or not an account exists.</summary>
    PasswordLogin,

    /// <summary>MFA challenge completion. The subject is the user id bound to a valid challenge.</summary>
    MfaChallenge,

    /// <summary>
    /// Step-up verification with an MFA code by an already authenticated user. The subject is the
    /// user id; the budget is separate from <see cref="MfaChallenge"/> so a login lockout cannot be
    /// triggered or cleared through the step-up surface.
    /// </summary>
    StepUpMfa,

    /// <summary>
    /// Re-verification of the current password by an already authenticated user (step-up and
    /// change-password share one budget so the guess allowance cannot be multiplied across surfaces).
    /// The subject is the user id.
    /// </summary>
    PasswordReverification
}

/// <summary>
/// Outcome of asking to start a credential attempt. The default value is
/// "allowed", so a test double that returns <c>default</c> never blocks.
/// </summary>
public readonly record struct LoginThrottleDecision(bool IsLocked, TimeSpan RetryAfter)
{
    public static LoginThrottleDecision Allowed => default;

    public static LoginThrottleDecision Locked(TimeSpan retryAfter) => new(true, retryAfter);
}

/// <summary>
/// Per-account plus per-IP lockout for anonymous credential verification.
/// Identity facts are keyed by a one-way hash; no email, IP or secret is
/// stored in clear. The caller must answer a locked decision with the same
/// generic error for every subject so lockout can never be used to learn
/// whether an account exists.
/// </summary>
public interface ILoginThrottle
{
    /// <summary>
    /// Reserves one attempt for the subject (atomic, so parallel guesses cannot
    /// overshoot the limit) after checking the IP lock. Call before verifying
    /// the credential, and only verify when the decision is not locked.
    /// </summary>
    Task<LoginThrottleDecision> BeginAttemptAsync(
        LoginThrottleScope scope, string subject, CancellationToken cancellationToken = default);

    /// <summary>The credential was wrong: counts against the IP and locks the subject when its limit is reached.</summary>
    Task RecordFailureAsync(
        LoginThrottleScope scope, string subject, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts a failure that cannot be tied to a subject (for example an unknown
    /// or expired challenge token) against the IP only.
    /// </summary>
    Task RecordAnonymousFailureAsync(CancellationToken cancellationToken = default);

    /// <summary>The credential was right: clears the subject counter. The IP counter is intentionally not reset.</summary>
    Task RecordSuccessAsync(
        LoginThrottleScope scope, string subject, CancellationToken cancellationToken = default);
}

public static class LoginThrottleErrors
{
    public const string Code = "identity.auth.too-many-attempts";

    /// <summary>Same message and code for any subject, known or not.</summary>
    public static ApplicationError TooManyAttempts(TimeSpan retryAfter) => new(
        Code,
        "Too many attempts. Try again later.",
        ApplicationErrorType.TooManyRequests,
        RetryAfter: retryAfter);
}
