using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Mfa.Abstractions;
using Notrelix.Domain.Identity.Mfa;
using Notrelix.Application.Features.Identity.Mfa.DTOs;

namespace Notrelix.Application.Features.Identity.Mfa.Commands.RegenerateRecoveryCodes;

public sealed record RegenerateRecoveryCodesCommand
    : ICommand<Result<MfaEnrollmentVerifyResult>>,
      ITransactionalRequest,
      IGlobalRequest,
      IAuthenticatedRequest;

public sealed class RegenerateRecoveryCodesCommandHandler
    : IRequestHandler<RegenerateRecoveryCodesCommand, Result<MfaEnrollmentVerifyResult>>
{
    private readonly IIdentityDbContext _context;
    private readonly ICurrentRequestContext _currentUser;
    private readonly IMfaRecoveryCodeGenerator _recoveryGenerator;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<RegenerateRecoveryCodesCommandHandler> _logger;

    public RegenerateRecoveryCodesCommandHandler(
        IIdentityDbContext context,
        ICurrentRequestContext currentUser,
        IMfaRecoveryCodeGenerator recoveryGenerator,
        IDateTimeProvider dateTimeProvider,
        ILogger<RegenerateRecoveryCodesCommandHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _recoveryGenerator = recoveryGenerator;
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
}
