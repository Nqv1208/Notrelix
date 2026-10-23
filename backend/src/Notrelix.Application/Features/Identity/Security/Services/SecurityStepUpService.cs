using System.Security.Cryptography;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Auth.Throttling;
using Notrelix.Application.Features.Identity.Mfa;
using Notrelix.Application.Features.Identity.Mfa.Abstractions;
using Notrelix.Application.Features.Identity.Mfa.DTOs;
using Notrelix.Application.Features.Identity.Security.Abstractions;
using Notrelix.Application.Features.Identity.Security.DTOs;
using Notrelix.Domain.Identity.Mfa;

namespace Notrelix.Application.Features.Identity.Security.Services;

/// <summary>
/// Implements the canonical step-up verification boundary.
/// Factor selection: enrolled MFA factor wins; otherwise password credential; otherwise OAuth re-authentication.
/// An UNVERIFIED MFA challenge only authorizes factor verification; only a VERIFIED,
/// single-use proof (stored in IStepUpProofStore) may authorize a sensitive mutation.
/// </summary>
public sealed class SecurityStepUpService : ISecurityStepUpService
{
    private readonly IIdentityDbContext _context;
    private readonly IMfaChallengeStore _challengeStore;
    private readonly IStepUpProofStore _proofStore;
    private readonly IMfaCodeVerifier _codeVerifier;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILoginThrottle _throttle;

    private const string DummyPasswordHash = "$2b$12$l21rZMRnrPl/Lfm2kVzYOuxlKgQzbwMzEvK7cOBZI40eJ42/FIuh2";

    public SecurityStepUpService(
        IIdentityDbContext context,
        IMfaChallengeStore challengeStore,
        IStepUpProofStore proofStore,
        IMfaCodeVerifier codeVerifier,
        IPasswordHasher passwordHasher,
        IDateTimeProvider dateTimeProvider,
        ILoginThrottle throttle)
    {
        _context = context;
        _challengeStore = challengeStore;
        _proofStore = proofStore;
        _codeVerifier = codeVerifier;
        _passwordHasher = passwordHasher;
        _dateTimeProvider = dateTimeProvider;
        _throttle = throttle;
    }

    public async Task<Result<StepUpRequirementResult>> GetRequirementAsync(
        Guid userId, Guid sessionId, StepUpPurpose purpose, CancellationToken ct)
    {
        var hasActiveMfa = await _context.UserMfaMethods
            .AnyAsync(m => m.UserId == userId && m.Status == MfaMethodStatus.Active, ct);

        if (hasActiveMfa)
        {
            var (token, payload) = await MfaChallengeFactory.CreateAsync(
                _challengeStore, userId, StepUpPurposeMapping.ToChallengePurpose(purpose),
                _dateTimeProvider.UtcNow, ct, sessionId);

            return Result<StepUpRequirementResult>.Success(new StepUpRequirementResult(
                StepUpRequiredFactor.MfaChallenge, token, payload.ExpiresAt));
        }

        var hasPasswordCredential = await _context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == userId && u.HasPasswordCredential, ct);

        if (hasPasswordCredential)
        {
            return Result<StepUpRequirementResult>.Success(new StepUpRequirementResult(
                StepUpRequiredFactor.Password, null, null));
        }

        return Result<StepUpRequirementResult>.Success(new StepUpRequirementResult(
            StepUpRequiredFactor.OAuth, null, null));
    }

    public async Task<Result<StepUpProofResult>> CompleteMfaAsync(
        Guid userId, Guid sessionId, StepUpPurpose purpose, string challengeToken, string code, CancellationToken ct)
    {
        // A signed-in session does not make code guessing free: reserve an attempt for the
        // user (atomic, so parallel guesses cannot overshoot) before any code is checked.
        var subject = userId.ToString("N");
        var gate = await _throttle.BeginAttemptAsync(LoginThrottleScope.StepUpMfa, subject, ct);
        if (gate.IsLocked)
        {
            return Result<StepUpProofResult>.Failure(LoginThrottleErrors.TooManyAttempts(gate.RetryAfter));
        }

        var now = _dateTimeProvider.UtcNow;

        var challenge = await _challengeStore.PeekAsync(challengeToken, ct);
        if (challenge is null || challenge.ExpiresAt < now || !Matches(challenge, userId, sessionId, purpose))
        {
            await _throttle.RecordFailureAsync(LoginThrottleScope.StepUpMfa, subject, ct);
            return InvalidProofResult();
        }

        var attempt = await _challengeStore.RecordAttemptAsync(
            challengeToken, MfaPolicy.ChallengeMaxAttempts, MfaPolicy.ChallengeTtl, ct);

        if (attempt.Exceeded)
        {
            await _throttle.RecordFailureAsync(LoginThrottleScope.StepUpMfa, subject, ct);
            return InvalidProofResult();
        }

        var verified = await _codeVerifier.VerifyAsync(userId, code, now, ct);
        if (!verified)
        {
            await _throttle.RecordFailureAsync(LoginThrottleScope.StepUpMfa, subject, ct);
            return Result<StepUpProofResult>.Failure(new ApplicationError(
                "identity.security.step-up-invalid-code",
                "Invalid step-up verification code.",
                ApplicationErrorType.Validation));
        }

        var consumed = await _challengeStore.ConsumeAsync(challengeToken, ct);
        if (consumed is null
            || consumed.ChallengeId != challenge.ChallengeId
            || consumed.ExpiresAt < now
            || !Matches(consumed, userId, sessionId, purpose))
        {
            await _throttle.RecordFailureAsync(LoginThrottleScope.StepUpMfa, subject, ct);
            return InvalidProofResult();
        }

        await _throttle.RecordSuccessAsync(LoginThrottleScope.StepUpMfa, subject, ct);
        return await IssueProofAsync(userId, sessionId, purpose, now, ct);
    }

    public async Task<Result<StepUpProofResult>> CompletePasswordAsync(
        Guid userId, Guid sessionId, StepUpPurpose purpose, string password, CancellationToken ct)
    {
        var subject = userId.ToString("N");
        var gate = await _throttle.BeginAttemptAsync(LoginThrottleScope.PasswordReverification, subject, ct);
        if (gate.IsLocked)
        {
            return Result<StepUpProofResult>.Failure(LoginThrottleErrors.TooManyAttempts(gate.RetryAfter));
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null || !user.HasPasswordCredential)
        {
            return InvalidProofResult();
        }

        var passwordValid = _passwordHasher.VerifyPassword(password, user.PasswordHash ?? DummyPasswordHash);
        if (!passwordValid)
        {
            await _throttle.RecordFailureAsync(LoginThrottleScope.PasswordReverification, subject, ct);
            return Result<StepUpProofResult>.Failure(new ApplicationError(
                "identity.security.step-up-invalid-password",
                "Invalid step-up password.",
                ApplicationErrorType.Validation));
        }

        await _throttle.RecordSuccessAsync(LoginThrottleScope.PasswordReverification, subject, ct);
        return await IssueProofAsync(userId, sessionId, purpose, _dateTimeProvider.UtcNow, ct);
    }

    public async Task<Result<StepUpProofResult>> GrantOAuthProofAsync(
        Guid userId, Guid sessionId, StepUpPurpose purpose, CancellationToken ct)
        => await IssueProofAsync(userId, sessionId, purpose, _dateTimeProvider.UtcNow, ct);

    public async Task<Result> ConsumeAsync(
        string proofToken, Guid userId, Guid sessionId, StepUpPurpose purpose, CancellationToken ct)
    {
        var now = _dateTimeProvider.UtcNow;

        var proof = await _proofStore.ConsumeAsync(proofToken, ct);
        if (proof is null
            || proof.UserId != userId
            || proof.SessionId != sessionId
            || proof.Purpose != purpose
            || proof.ExpiresAt < now)
        {
            return Result.Failure(new ApplicationError(
                "identity.security.step-up-invalid",
                "Step-up verification is invalid or expired.",
                ApplicationErrorType.Validation));
        }

        return Result.Success();
    }

    private static bool Matches(MfaChallengePayload payload, Guid userId, Guid sessionId, StepUpPurpose purpose)
        => payload.UserId == userId
           && payload.SessionId == sessionId
           && payload.Purpose == StepUpPurposeMapping.ToChallengePurpose(purpose);

    private static Result<StepUpProofResult> InvalidProofResult() => Result<StepUpProofResult>.Failure(
        new ApplicationError(
            "identity.security.step-up-invalid",
            "Step-up verification is invalid or expired.",
            ApplicationErrorType.Validation));

    private async Task<Result<StepUpProofResult>> IssueProofAsync(
        Guid userId, Guid sessionId, StepUpPurpose purpose, DateTimeOffset now, CancellationToken ct)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var payload = new StepUpProofPayload(userId, sessionId, purpose, now, now.Add(SecurityStepUpPolicy.ProofTtl));
        await _proofStore.StoreAsync(token, payload, SecurityStepUpPolicy.ProofTtl, ct);
        return Result<StepUpProofResult>.Success(new StepUpProofResult(token, payload.ExpiresAt));
    }
}
