using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Mfa.Abstractions;
using Notrelix.Application.Features.Identity.Security.Abstractions;
using Notrelix.Application.Features.Identity.Security.DTOs;
using Notrelix.Domain.Identity.Mfa;
using Notrelix.Application.Features.Identity.Mfa.DTOs;
using Notrelix.Domain.Identity.Security;

namespace Notrelix.Application.Features.Identity.Mfa.Commands.VerifyMfaEnrollment;

/// <summary>
/// Activates the pending authenticator. Needs its own single-use step-up proof of purpose EnrollMfa,
/// and when the factor becomes active every other session of the person ends (the current one stays).
/// </summary>
public sealed record VerifyMfaEnrollmentCommand
    : ICommand<Result<MfaEnrollmentActivationResult>>,
      IWriteRequest,
      IGlobalRequest,
      IAuthenticatedRequest
{
    public required Guid MfaMethodId { get; init; }
    public required string Code { get; init; }

    /// <summary>Single-use step-up proof for the EnrollMfa purpose.</summary>
    public string StepUpToken { get; init; } = string.Empty;
}

public sealed class VerifyMfaEnrollmentCommandHandler
    : IRequestHandler<VerifyMfaEnrollmentCommand, Result<MfaEnrollmentActivationResult>>
{
    private readonly IIdentityDbContext _context;
    private readonly ICurrentRequestContext _currentUser;
    private readonly IMfaTotpService _totp;
    private readonly IMfaRecoveryCodeGenerator _recoveryGenerator;
    private readonly ISecurityStepUpService _stepUpService;
    private readonly IJwtBlacklistService _jwtBlacklist;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ISecurityActivityRecorder _activity;
    private readonly ILogger<VerifyMfaEnrollmentCommandHandler> _logger;

    private static readonly TimeSpan SessionRevocationMarkerTtl = TimeSpan.FromHours(24);

    public VerifyMfaEnrollmentCommandHandler(
        IIdentityDbContext context,
        ICurrentRequestContext currentUser,
        IMfaTotpService totp,
        IMfaRecoveryCodeGenerator recoveryGenerator,
        ISecurityStepUpService stepUpService,
        IJwtBlacklistService jwtBlacklist,
        IDateTimeProvider dateTimeProvider,
        ISecurityActivityRecorder activity,
        ILogger<VerifyMfaEnrollmentCommandHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _totp = totp;
        _recoveryGenerator = recoveryGenerator;
        _stepUpService = stepUpService;
        _jwtBlacklist = jwtBlacklist;
        _dateTimeProvider = dateTimeProvider;
        _activity = activity;
        _logger = logger;
    }

    public async Task<Result<MfaEnrollmentActivationResult>> Handle(
        VerifyMfaEnrollmentCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        if (string.IsNullOrWhiteSpace(request.StepUpToken) || _currentUser.SessionId is not { } currentSessionId)
        {
            return Result<MfaEnrollmentActivationResult>.Failure(new ApplicationError(
                "identity.security.step-up-required",
                "Strong verification is required for this action.",
                ApplicationErrorType.PreconditionFailed));
        }

        var method = await _context.UserMfaMethods
            .FirstOrDefaultAsync(m => m.Id == request.MfaMethodId && m.UserId == userId, cancellationToken);

        if (method is null || method.Status != MfaMethodStatus.PendingVerification)
        {
            return Result<MfaEnrollmentActivationResult>.Failure(new ApplicationError(
                "identity.mfa.enrollment-not-found",
                "MFA enrollment not found or already completed.",
                ApplicationErrorType.Validation));
        }

        var now = _dateTimeProvider.UtcNow;

        string unprotectedSecret;
        try
        {
            unprotectedSecret = _totp.UnprotectSecret(method.SecretRef!.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unprotect MFA secret for enrollment {MfaMethodId}", method.Id);
            return Result<MfaEnrollmentActivationResult>.Failure(new ApplicationError(
                "identity.mfa.secret-invalid",
                "MFA enrollment secret could not be verified.",
                ApplicationErrorType.BusinessRule));
        }

        if (!_totp.VerifyCode(unprotectedSecret, request.Code, now))
        {
            return Result<MfaEnrollmentActivationResult>.Failure(new ApplicationError(
                "identity.mfa.invalid-verification-code",
                "Invalid verification code.",
                ApplicationErrorType.Validation));
        }

        // After the code check so a mistyped code keeps the proof; before anything is activated.
        var stepUp = await _stepUpService.ConsumeAsync(
            request.StepUpToken, userId, currentSessionId, StepUpPurpose.EnrollMfa, cancellationToken);
        if (!stepUp.Succeeded)
        {
            return Result<MfaEnrollmentActivationResult>.Failure(stepUp.TypedErrors);
        }

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
        _activity.Record(userId, SecurityActivityType.MfaEnabled, now, _currentUser.SessionId);

        await InvalidateExistingBatchesAsync(userId, now, cancellationToken);
        var endedOthers = await EndOtherSessionsAsync(userId, currentSessionId, now, cancellationToken);

        var plaintextCodes = _recoveryGenerator.Generate(MfaPolicy.RecoveryCodeCount);
        var codeHashes = plaintextCodes.Select(_recoveryGenerator.Hash).ToArray();
        var batch = MfaRecoveryBatch.Create(userId, codeHashes, now, userId);
        _context.MfaRecoveryBatches.Add(batch);

        _logger.LogInformation("MFA enabled for {UserId} with recovery batch {BatchId}", userId, batch.Id);

        var result = new MfaEnrollmentActivationResult(method.Id, plaintextCodes, endedOthers);
        return Result<MfaEnrollmentActivationResult>.Success(result);
    }

    /// <summary>A factor that just became active ends every other session; this one stays.</summary>
    private async Task<int> EndOtherSessionsAsync(
        Guid userId, Guid currentSessionId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var others = await _context.Sessions
            .Where(s =>
                s.UserId == userId &&
                s.Id != currentSessionId &&
                s.Status == SessionStatus.Active &&
                s.ExpiresAt > now)
            .ToListAsync(cancellationToken);

        foreach (var session in others)
        {
            session.Revoke(now, SessionRevocationReasons.MfaEnrolled);
            await _jwtBlacklist.RevokeSessionBeforeAsync(session.Id, now, SessionRevocationMarkerTtl);
        }

        if (others.Count > 0)
        {
            _activity.Record(userId, SecurityActivityType.OtherSessionsRevoked, now, currentSessionId);
        }

        return others.Count;
    }

    private async Task InvalidateExistingBatchesAsync(
        Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var activeBatches = await _context.MfaRecoveryBatches
            .Where(b => b.UserId == userId && b.InvalidatedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var batch in activeBatches)
        {
            batch.Invalidate(now, userId);
        }
    }
}
