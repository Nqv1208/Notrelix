using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Mfa.Abstractions;
using Notrelix.Application.Features.Identity.Mfa.DTOs;
using Notrelix.Application.Features.Identity.Security.Abstractions;
using Notrelix.Application.Features.Identity.Security.DTOs;
using Notrelix.Domain.Identity.Mfa;

namespace Notrelix.Application.Features.Identity.Mfa.Commands.RegenerateRecoveryCodes;

public sealed record RegenerateRecoveryCodesCommand
    : ICommand<Result<MfaEnrollmentVerifyResult>>,
      IWriteRequest,
      IGlobalRequest,
      IAuthenticatedRequest
{
    /// <summary>Single-use step-up proof for the RegenerateRecoveryCodes purpose (TOTP, recovery code or password).</summary>
    public required string StepUpToken { get; init; }
}

public sealed class RegenerateRecoveryCodesCommandHandler
    : IRequestHandler<RegenerateRecoveryCodesCommand, Result<MfaEnrollmentVerifyResult>>
{
    private readonly IIdentityDbContext _context;
    private readonly ICurrentRequestContext _currentUser;
    private readonly IMfaRecoveryCodeGenerator _recoveryGenerator;
    private readonly ISecurityStepUpService _stepUpService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<RegenerateRecoveryCodesCommandHandler> _logger;

    public RegenerateRecoveryCodesCommandHandler(
        IIdentityDbContext context,
        ICurrentRequestContext currentUser,
        IMfaRecoveryCodeGenerator recoveryGenerator,
        ISecurityStepUpService stepUpService,
        IDateTimeProvider dateTimeProvider,
        ILogger<RegenerateRecoveryCodesCommandHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _recoveryGenerator = recoveryGenerator;
        _stepUpService = stepUpService;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result<MfaEnrollmentVerifyResult>> Handle(
        RegenerateRecoveryCodesCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var hasActiveMethod = await _context.UserMfaMethods
            .AnyAsync(m => m.UserId == userId && m.Status == MfaMethodStatus.Active, cancellationToken);

        if (!hasActiveMethod)
        {
            return Result<MfaEnrollmentVerifyResult>.Failure(new ApplicationError(
                "identity.mfa.not-enabled",
                "MFA is not enabled for this account.",
                ApplicationErrorType.PreconditionFailed));
        }

        var stepUp = await ConsumeStepUpAsync(userId, request.StepUpToken, cancellationToken);
        if (!stepUp.Succeeded)
        {
            return Result<MfaEnrollmentVerifyResult>.Failure(stepUp.TypedErrors);
        }

        var now = _dateTimeProvider.UtcNow;

        var activeBatches = await _context.MfaRecoveryBatches
            .Where(b => b.UserId == userId && b.InvalidatedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var batch in activeBatches)
        {
            batch.Invalidate(now, userId);
        }

        var plaintextCodes = _recoveryGenerator.Generate(MfaPolicy.RecoveryCodeCount);
        var codeHashes = plaintextCodes.Select(_recoveryGenerator.Hash).ToArray();
        var newBatch = MfaRecoveryBatch.Create(userId, codeHashes, now, userId);
        _context.MfaRecoveryBatches.Add(newBatch);

        _logger.LogInformation("Recovery codes regenerated for {UserId} (batch {BatchId})", userId, newBatch.Id);

        var result = new MfaEnrollmentVerifyResult(newBatch.Id, plaintextCodes);
        return Result<MfaEnrollmentVerifyResult>.Success(result);
    }

    private async Task<Result> ConsumeStepUpAsync(Guid userId, string stepUpToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(stepUpToken))
        {
            return Result.Failure(new ApplicationError(
                "identity.security.step-up-required",
                "Strong verification is required for this action.",
                ApplicationErrorType.PreconditionFailed));
        }

        if (_currentUser.SessionId is not { } sessionId)
        {
            return Result.Failure(new ApplicationError(
                "identity.security.step-up-required",
                "Strong verification is required for this action.",
                ApplicationErrorType.PreconditionFailed));
        }

        return await _stepUpService.ConsumeAsync(
            stepUpToken, userId, sessionId, StepUpPurpose.RegenerateRecoveryCodes, cancellationToken);
    }
}