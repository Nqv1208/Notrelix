using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Auth.Services;
using Notrelix.Application.Features.Identity.Auth.Throttling;
using Notrelix.Application.Features.Identity.Mfa.Abstractions;
using Notrelix.Application.Features.Identity.Mfa.DTOs;
using Notrelix.Application.Features.Identity.Security.Abstractions;
using Notrelix.Domain.Identity.Security;

namespace Notrelix.Application.Features.Identity.Mfa.Commands.CompleteMfaChallenge;

public sealed record CompleteMfaChallengeCommand
    : ICommand<Result<AuthResult>>,
      IWriteRequest,
      IGlobalRequest,
      IAnonymousRequest
{
    public required string ChallengeToken { get; init; }
    public required string Code { get; init; }
}

public sealed class CompleteMfaChallengeCommandHandler
    : IRequestHandler<CompleteMfaChallengeCommand, Result<AuthResult>>
{
    private readonly IIdentityDbContext _context;
    private readonly IMfaChallengeStore _challengeStore;
    private readonly IMfaCodeVerifier _codeVerifier;
    private readonly ILoginThrottle _throttle;
    private readonly ISecurityActivityRecorder _activity;
    private readonly IAuthSessionIssuer _sessionIssuer;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<CompleteMfaChallengeCommandHandler> _logger;
    private readonly CredentialScope? _credentialScope;

    public CompleteMfaChallengeCommandHandler(
        IIdentityDbContext context,
        IMfaChallengeStore challengeStore,
        IMfaCodeVerifier codeVerifier,
        ILoginThrottle throttle,
        ISecurityActivityRecorder activity,
        IAuthSessionIssuer sessionIssuer,
        IDateTimeProvider dateTimeProvider,
        ILogger<CompleteMfaChallengeCommandHandler> logger,
        CredentialScope? credentialScope = null)
    {
        _credentialScope = credentialScope;
        _context = context;
        _challengeStore = challengeStore;
        _codeVerifier = codeVerifier;
        _throttle = throttle;
        _activity = activity;
        _sessionIssuer = sessionIssuer;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result<AuthResult>> Handle(
        CompleteMfaChallengeCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;

        var challenge = await _challengeStore.PeekAsync(request.ChallengeToken, cancellationToken);
        if (challenge is null || challenge.ExpiresAt < now)
        {
            // No subject to charge: count it against the client IP only.
            await _throttle.RecordAnonymousFailureAsync(cancellationToken);
            return InvalidChallenge();
        }

        // Login MFA completion accepts only login purposes. A step-up challenge
        // must never complete login (and must not reveal that it was valid).
        if (challenge.Purpose is not (MfaChallengePurpose.PasswordLogin or MfaChallengePurpose.OAuthLogin))
        {
            _logger.LogWarning("MFA challenge with purpose {Purpose} rejected by login completion", challenge.Purpose);
            await _throttle.RecordAnonymousFailureAsync(cancellationToken);
            return InvalidChallenge();
        }

        // The request now acts for exactly the user the valid challenge belongs to (the challenge record is
        // server-held, so the caller does not choose the user).
        if (_credentialScope is not null)
            await _credentialScope.EnterForServerBoundUserAsync(challenge.UserId, cancellationToken);

        // The subject is the user bound to a valid challenge, so a locked answer
        // is only ever produced for someone who already passed the password step.
        var subject = challenge.UserId.ToString("N");
        var throttle = await _throttle.BeginAttemptAsync(LoginThrottleScope.MfaChallenge, subject, cancellationToken);
        if (throttle.IsLocked)
        {
            _logger.LogWarning("MFA completion rejected for {UserId}: attempt limit reached (retry in {RetryAfterSeconds}s)",
                challenge.UserId, (int)throttle.RetryAfter.TotalSeconds);
            return Result<AuthResult>.Failure(LoginThrottleErrors.TooManyAttempts(throttle.RetryAfter));
        }

        var attempt = await _challengeStore.RecordAttemptAsync(
            request.ChallengeToken, MfaPolicy.ChallengeMaxAttempts, MfaPolicy.ChallengeTtl, cancellationToken);

        if (attempt.Exceeded)
        {
            // The challenge was removed by the attempt budget: it can never be completed again.
            await _throttle.RecordFailureAsync(LoginThrottleScope.MfaChallenge, subject, cancellationToken);
            _activity.Record(challenge.UserId, SecurityActivityType.SignInFailed, now);
            return InvalidChallenge();
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == challenge.UserId, cancellationToken);

        if (user is null || user.Status != UserStatus.Active)
        {
            return Result<AuthResult>.Failure(new ApplicationError(
                "identity.mfa.account-inactive",
                "Account is not active.",
                ApplicationErrorType.Conflict));
        }

        var verified = await _codeVerifier.VerifyAsync(user.Id, request.Code, now, cancellationToken);

        if (!verified)
        {
            await _throttle.RecordFailureAsync(LoginThrottleScope.MfaChallenge, subject, cancellationToken);
            _activity.Record(user.Id, SecurityActivityType.SignInFailed, now);
            _logger.LogWarning("MFA challenge failed verification for {UserId} (purpose {Purpose})",
                user.Id, challenge.Purpose);

            // The last allowed try failed: the challenge dies now, not at the next attempt, so no
            // later code (right or wrong) can open it and the person signs in again.
            if (attempt.Attempts >= MfaPolicy.ChallengeMaxAttempts)
            {
                await _challengeStore.ConsumeAsync(request.ChallengeToken, cancellationToken);
                return InvalidChallenge();
            }

            return Result<AuthResult>.Failure(new ApplicationError(
                "identity.mfa.invalid-code",
                "Invalid MFA code.",
                ApplicationErrorType.Validation));
        }

        var consumed = await _challengeStore.ConsumeAsync(request.ChallengeToken, cancellationToken);
        if (consumed is null
            || consumed.ChallengeId != challenge.ChallengeId
            || consumed.ExpiresAt < now
            || consumed.Purpose != challenge.Purpose)
        {
            await _throttle.RecordFailureAsync(LoginThrottleScope.MfaChallenge, subject, cancellationToken);
            return InvalidChallenge();
        }

        await _throttle.RecordSuccessAsync(LoginThrottleScope.MfaChallenge, subject, cancellationToken);

        user.RecordLogin(now);
        var authResult = await _sessionIssuer.IssueAsync(user, now, cancellationToken);

        _logger.LogInformation("MFA challenge completed for {UserId} (purpose {Purpose})",
            user.Id, challenge.Purpose);

        return Result<AuthResult>.Success(authResult);
    }

    private static Result<AuthResult> InvalidChallenge() => Result<AuthResult>.Failure(CompleteMfaChallengeErrors.ChallengeInvalid);
}

public static class CompleteMfaChallengeErrors
{
    public const string ChallengeInvalidCode = "identity.mfa.challenge-invalid";

    /// <summary>The challenge is unknown, expired, exhausted or of another purpose.</summary>
    public static ApplicationError ChallengeInvalid { get; } = new(
        ChallengeInvalidCode,
        "MFA challenge is invalid or expired.",
        ApplicationErrorType.Validation);
}
