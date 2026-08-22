using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Mfa.Abstractions;
using Notrelix.Domain.Identity.Mfa;
using Notrelix.Application.Features.Identity.Mfa.DTOs;

namespace Notrelix.Application.Features.Identity.Mfa.Commands.StartMfaEnrollment;

public sealed record StartMfaEnrollmentCommand
    : ICommand<Result<MfaEnrollmentStartResult>>,
      ITransactionalRequest,
      IGlobalRequest,
      IAuthenticatedRequest;

public sealed class StartMfaEnrollmentCommandHandler
    : IRequestHandler<StartMfaEnrollmentCommand, Result<MfaEnrollmentStartResult>>
{
    private readonly IIdentityDbContext _context;
    private readonly ICurrentRequestContext _currentUser;
    private readonly IMfaTotpService _totp;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<StartMfaEnrollmentCommandHandler> _logger;

    public StartMfaEnrollmentCommandHandler(
        IIdentityDbContext context,
        ICurrentRequestContext currentUser,
        IMfaTotpService totp,
        IDateTimeProvider dateTimeProvider,
        ILogger<StartMfaEnrollmentCommandHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _totp = totp;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result<MfaEnrollmentStartResult>> Handle(
        StartMfaEnrollmentCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            return Result<MfaEnrollmentStartResult>.Failure(new ApplicationError(
                "identity.mfa.user-not-found",
                "User not found.",
                ApplicationErrorType.NotFound));
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
}
