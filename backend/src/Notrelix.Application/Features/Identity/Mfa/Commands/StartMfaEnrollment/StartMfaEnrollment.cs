using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Mfa.Abstractions;
using Notrelix.Application.Features.Identity.Security.Abstractions;
using Notrelix.Application.Features.Identity.Security.DTOs;
using Notrelix.Domain.Identity.Mfa;
using Notrelix.Application.Features.Identity.Mfa.DTOs;

namespace Notrelix.Application.Features.Identity.Mfa.Commands.StartMfaEnrollment;

/// <summary>
/// Starts a pending authenticator. A session alone may not do it: it needs a single-use
/// step-up proof of purpose EnrollMfa, so a stolen session cannot enrol its own factor.
/// </summary>
public sealed record StartMfaEnrollmentCommand
    : ICommand<Result<MfaEnrollmentStartResult>>,
      IWriteRequest,
      IGlobalRequest,
      IAuthenticatedRequest
{
    /// <summary>Single-use step-up proof for the EnrollMfa purpose.</summary>
    public string StepUpToken { get; init; } = string.Empty;
}

public sealed class StartMfaEnrollmentCommandHandler
    : IRequestHandler<StartMfaEnrollmentCommand, Result<MfaEnrollmentStartResult>>
{
    private readonly IIdentityDbContext _context;
    private readonly ICurrentRequestContext _currentUser;
    private readonly IMfaTotpService _totp;
    private readonly ISecurityStepUpService _stepUpService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<StartMfaEnrollmentCommandHandler> _logger;

    public StartMfaEnrollmentCommandHandler(
        IIdentityDbContext context,
        ICurrentRequestContext currentUser,
        IMfaTotpService totp,
        ISecurityStepUpService stepUpService,
        IDateTimeProvider dateTimeProvider,
        ILogger<StartMfaEnrollmentCommandHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _totp = totp;
        _stepUpService = stepUpService;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result<MfaEnrollmentStartResult>> Handle(
        StartMfaEnrollmentCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        if (string.IsNullOrWhiteSpace(request.StepUpToken) || _currentUser.SessionId is not { } sessionId)
        {
            return Result<MfaEnrollmentStartResult>.Failure(StepUpRequired());
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            return Result<MfaEnrollmentStartResult>.Failure(SessionErrors.SessionUserMissing);
        }

        var hasActiveMethod = await _context.UserMfaMethods
            .AnyAsync(m => m.UserId == userId && m.Status == MfaMethodStatus.Active, cancellationToken);

        if (hasActiveMethod)
        {
            return Result<MfaEnrollmentStartResult>.Failure(new ApplicationError(
                "identity.mfa.already-enabled",
                "MFA is already enabled for this account. Disable it before re-enrolling.",
                ApplicationErrorType.Conflict));
        }

        // Checked above so a refused start keeps the proof; consumed before anything is created.
        var stepUp = await _stepUpService.ConsumeAsync(
            request.StepUpToken, userId, sessionId, StepUpPurpose.EnrollMfa, cancellationToken);
        if (!stepUp.Succeeded)
        {
            return Result<MfaEnrollmentStartResult>.Failure(stepUp.TypedErrors);
        }

        var abandoned = await _context.UserMfaMethods
            .Where(m => m.UserId == userId && m.Status == MfaMethodStatus.PendingVerification)
            .ToListAsync(cancellationToken);

        var now = _dateTimeProvider.UtcNow;
        foreach (var pending in abandoned)
        {
            pending.Disable(now);
        }

        var secret = _totp.GenerateSecretKey();
        var protectedSecret = _totp.ProtectSecret(secret);

        var method = UserMfaMethod.Create(
            userId,
            MfaMethodType.AuthenticatorApp,
            now,
            SecretRef.Create(protectedSecret));

        _context.UserMfaMethods.Add(method);

        _logger.LogInformation("MFA authenticator enrollment started for {UserId}", userId);

        var result = new MfaEnrollmentStartResult(
            method.Id,
            secret,
            _totp.BuildOtpAuthUri(secret, user.Email.Value, MfaPolicy.TotpIssuer));

        return Result<MfaEnrollmentStartResult>.Success(result);
    }

    private static ApplicationError StepUpRequired() => new(
        "identity.security.step-up-required",
        "Strong verification is required for this action.",
        ApplicationErrorType.PreconditionFailed);
}
