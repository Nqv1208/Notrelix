using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Mfa.Abstractions;
using Notrelix.Domain.Identity.Mfa;
using Notrelix.Application.Features.Identity.Mfa.DTOs;
using Notrelix.Domain.Identity.Security;

namespace Notrelix.Application.Features.Identity.Mfa.Commands.VerifyMfaEnrollment;

public sealed record VerifyMfaEnrollmentCommand
    : ICommand<Result<MfaEnrollmentVerifyResult>>,
      IWriteRequest,
      IGlobalRequest,
      IAuthenticatedRequest
{
    public required Guid MfaMethodId { get; init; }
    public required string Code { get; init; }
}

public sealed class VerifyMfaEnrollmentCommandHandler
    : IRequestHandler<VerifyMfaEnrollmentCommand, Result<MfaEnrollmentVerifyResult>>
{
    private readonly IIdentityDbContext _context;
    private readonly ICurrentRequestContext _currentUser;
    private readonly IMfaTotpService _totp;
    private readonly IMfaRecoveryCodeGenerator _recoveryGenerator;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<VerifyMfaEnrollmentCommandHandler> _logger;

    public VerifyMfaEnrollmentCommandHandler(
        IIdentityDbContext context,
        ICurrentRequestContext currentUser,
        IMfaTotpService totp,
        IMfaRecoveryCodeGenerator recoveryGenerator,
        IDateTimeProvider dateTimeProvider,
        ILogger<VerifyMfaEnrollmentCommandHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _totp = totp;
        _recoveryGenerator = recoveryGenerator;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result<MfaEnrollmentVerifyResult>> Handle(
        VerifyMfaEnrollmentCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var method = await _context.UserMfaMethods
            .FirstOrDefaultAsync(m => m.Id == request.MfaMethodId && m.UserId == userId, cancellationToken);

        if (method is null || method.Status != MfaMethodStatus.PendingVerification)
        {
            return Result<MfaEnrollmentVerifyResult>.Failure(new ApplicationError(
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
            return Result<MfaEnrollmentVerifyResult>.Failure(new ApplicationError(
                "identity.mfa.secret-invalid",
                "MFA enrollment secret could not be verified.",
                ApplicationErrorType.BusinessRule));
        }

        if (!_totp.VerifyCode(unprotectedSecret, request.Code, now))
        {
            return Result<MfaEnrollmentVerifyResult>.Failure(new ApplicationError(
                "identity.mfa.invalid-verification-code",
                "Invalid verification code.",
                ApplicationErrorType.Validation));
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

        await InvalidateExistingBatchesAsync(userId, now, cancellationToken);

        var plaintextCodes = _recoveryGenerator.Generate(MfaPolicy.RecoveryCodeCount);
        var codeHashes = plaintextCodes.Select(_recoveryGenerator.Hash).ToArray();
        var batch = MfaRecoveryBatch.Create(userId, codeHashes, now, userId);
        _context.MfaRecoveryBatches.Add(batch);

        _logger.LogInformation("MFA enabled for {UserId} with recovery batch {BatchId}", userId, batch.Id);

        var result = new MfaEnrollmentVerifyResult(method.Id, plaintextCodes);
        return Result<MfaEnrollmentVerifyResult>.Success(result);
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
