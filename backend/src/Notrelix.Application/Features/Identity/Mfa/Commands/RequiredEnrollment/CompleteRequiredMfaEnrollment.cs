using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Auth.Services;
using Notrelix.Application.Features.Identity.Mfa.Abstractions;
using Notrelix.Application.Features.Identity.Mfa.Commands.CompleteMfaChallenge;
using Notrelix.Application.Features.Identity.Mfa.DTOs;
using Notrelix.Application.Features.Identity.Security.Abstractions;
using Notrelix.Domain.Identity.Mfa;
using Notrelix.Domain.Identity.Security;

namespace Notrelix.Application.Features.Identity.Mfa.Commands.RequiredEnrollment;

/// <summary>
/// Activates the authenticator started by <see cref="StartRequiredMfaEnrollmentCommand"/> with its first
/// code, then finishes the sign-in that stopped for it: the session is issued here, with the one-time
/// recovery codes. A factor that just became active ends every other session of the person.
/// </summary>
public sealed record CompleteRequiredMfaEnrollmentCommand
    : ICommand<Result<AuthResult>>,
      IWriteRequest,
      IGlobalRequest,
      IAnonymousRequest
{
    public required string EnrollmentToken { get; init; }
    public required Guid MfaMethodId { get; init; }
    public required string Code { get; init; }
}

public sealed class CompleteRequiredMfaEnrollmentCommandHandler
    : IRequestHandler<CompleteRequiredMfaEnrollmentCommand, Result<AuthResult>>
{
    private static readonly TimeSpan SessionRevocationMarkerTtl = TimeSpan.FromHours(24);

    private readonly IIdentityDbContext _context;
    private readonly IMfaChallengeStore _challengeStore;
    private readonly IMfaTotpService _totp;
    private readonly IMfaRecoveryCodeGenerator _recoveryGenerator;
    private readonly IAuthSessionIssuer _sessionIssuer;
    private readonly IJwtBlacklistService _jwtBlacklist;
    private readonly ISecurityActivityRecorder _activity;
    private readonly ISecurityNotice _notice;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly CredentialScope? _credentialScope;

    public CompleteRequiredMfaEnrollmentCommandHandler(
        IIdentityDbContext context,
        IMfaChallengeStore challengeStore,
        IMfaTotpService totp,
        IMfaRecoveryCodeGenerator recoveryGenerator,
        IAuthSessionIssuer sessionIssuer,
        IJwtBlacklistService jwtBlacklist,
        ISecurityActivityRecorder activity,
        ISecurityNotice notice,
        IDateTimeProvider dateTimeProvider,
        CredentialScope? credentialScope = null)
    {
        _context = context;
        _challengeStore = challengeStore;
        _totp = totp;
        _recoveryGenerator = recoveryGenerator;
        _sessionIssuer = sessionIssuer;
        _jwtBlacklist = jwtBlacklist;
        _activity = activity;
        _notice = notice;
        _dateTimeProvider = dateTimeProvider;
        _credentialScope = credentialScope;
    }

    public async Task<Result<AuthResult>> Handle(
        CompleteRequiredMfaEnrollmentCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;

        var challenge = await _challengeStore.PeekAsync(request.EnrollmentToken, cancellationToken);
        if (challenge is null
            || challenge.ExpiresAt < now
            || challenge.Purpose != MfaChallengePurpose.RequiredEnrollment)
        {
            return Invalid();
        }

        if (_credentialScope is not null)
            await _credentialScope.EnterForServerBoundUserAsync(challenge.UserId, cancellationToken);

        var attempt = await _challengeStore.RecordAttemptAsync(
            request.EnrollmentToken, MfaPolicy.ChallengeMaxAttempts, MfaPolicy.EnrollmentTtl, cancellationToken);
        if (attempt.Exceeded)
            return Invalid();

        var userId = challenge.UserId;
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null || user.Status != UserStatus.Active)
            return Invalid();

        var method = await _context.UserMfaMethods
            .FirstOrDefaultAsync(m => m.Id == request.MfaMethodId && m.UserId == userId, cancellationToken);
        if (method is null || method.Status != MfaMethodStatus.PendingVerification)
        {
            return Result<AuthResult>.Failure(new ApplicationError(
                "identity.mfa.enrollment-not-found",
                "MFA enrollment not found or already completed.",
                ApplicationErrorType.Validation));
        }

        if (await _context.UserMfaMethods
                .AnyAsync(m => m.UserId == userId && m.Status == MfaMethodStatus.Active, cancellationToken))
        {
            return Invalid();
        }

        string secret;
        try
        {
            secret = _totp.UnprotectSecret(method.SecretRef!.Value);
        }
        catch (Exception)
        {
            return Result<AuthResult>.Failure(new ApplicationError(
                "identity.mfa.secret-invalid",
                "MFA enrollment secret could not be verified.",
                ApplicationErrorType.BusinessRule));
        }

        if (!_totp.VerifyCode(secret, request.Code, now))
        {
            // The last allowed try failed: the token dies now, so the person signs in again.
            if (attempt.Attempts >= MfaPolicy.ChallengeMaxAttempts)
            {
                await _challengeStore.ConsumeAsync(request.EnrollmentToken, cancellationToken);
                return Invalid();
            }

            return Result<AuthResult>.Failure(new ApplicationError(
                "identity.mfa.invalid-verification-code",
                "Invalid verification code.",
                ApplicationErrorType.Validation));
        }

        // Atomic: of two requests holding the token, one finishes the sign-in.
        var consumed = await _challengeStore.ConsumeAsync(request.EnrollmentToken, cancellationToken);
        if (consumed is null || consumed.ChallengeId != challenge.ChallengeId)
            return Invalid();

        method.Verify(now);
        method.SetAsPrimary(now);

        var settings = await _context.UserSecuritySettings
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
        if (settings is null)
        {
            settings = UserSecuritySettings.Create(userId, now);
            _context.UserSecuritySettings.Add(settings);
        }

        settings.EnableMfa(MfaMethodType.AuthenticatorApp, now);
        _activity.Record(userId, SecurityActivityType.MfaEnabled, now);
        await _notice.QueueAsync(userId, SecurityNoticeKind.MfaEnabled, now, null, cancellationToken);

        var activeBatches = await _context.MfaRecoveryBatches
            .Where(b => b.UserId == userId && b.InvalidatedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var old in activeBatches)
        {
            old.Invalidate(now, userId);
        }

        var codes = _recoveryGenerator.Generate(MfaPolicy.RecoveryCodeCount);
        _context.MfaRecoveryBatches.Add(
            MfaRecoveryBatch.Create(userId, codes.Select(_recoveryGenerator.Hash).ToArray(), now, userId));

        // The new session is not saved yet, so every session found here predates this sign-in.
        var others = await _context.Sessions
            .Where(s => s.UserId == userId && s.Status == SessionStatus.Active && s.ExpiresAt > now)
            .ToListAsync(cancellationToken);
        foreach (var session in others)
        {
            session.Revoke(now, SessionRevocationReasons.MfaEnrolled);
            await _jwtBlacklist.RevokeSessionBeforeAsync(session.Id, now, SessionRevocationMarkerTtl);
        }

        user.RecordLogin(now);
        var authResult = await _sessionIssuer.IssueAsync(user, now, cancellationToken);
        return Result<AuthResult>.Success(authResult with { RecoveryCodes = codes });
    }

    private static Result<AuthResult> Invalid() => Result<AuthResult>.Failure(CompleteMfaChallengeErrors.ChallengeInvalid);
}
