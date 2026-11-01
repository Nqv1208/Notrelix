using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Mfa.Abstractions;
using Notrelix.Domain.Identity.Mfa;

namespace Notrelix.Application.Features.Identity.Mfa.Services;

/// <summary>
/// TOTP-or-recovery verification against active MFA factors.
/// Extraction of the shared primitive originally owned by MFA challenge completion.
/// </summary>
public sealed class MfaCodeVerifier : IMfaCodeVerifier
{
    private readonly IIdentityDbContext _context;
    private readonly IMfaTotpService _totp;
    private readonly IMfaRecoveryCodeGenerator _recoveryGenerator;
    private readonly IMfaCodeUseGuard _useGuard;

    public MfaCodeVerifier(
        IIdentityDbContext context,
        IMfaTotpService totp,
        IMfaRecoveryCodeGenerator recoveryGenerator,
        IMfaCodeUseGuard useGuard)
    {
        _context = context;
        _totp = totp;
        _recoveryGenerator = recoveryGenerator;
        _useGuard = useGuard;
    }

    public async Task<bool> VerifyAsync(Guid userId, string code, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (await TryVerifyTotpAsync(userId, code, now, cancellationToken))
        {
            return true;
        }

        return await TryVerifyRecoveryCodeAsync(userId, code, now, cancellationToken);
    }

    private async Task<bool> TryVerifyTotpAsync(
        Guid userId, string code, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var method = await _context.UserMfaMethods
            .FirstOrDefaultAsync(m =>
                m.UserId == userId &&
                m.Status == MfaMethodStatus.Active &&
                m.Type == MfaMethodType.AuthenticatorApp,
                cancellationToken);

        if (method is null || method.SecretRef is null)
        {
            return false;
        }

        string unprotectedSecret;
        try
        {
            unprotectedSecret = _totp.UnprotectSecret(method.SecretRef.Value);
        }
        catch (Exception)
        {
            return false;
        }

        var timeStep = _totp.FindMatchingTimeStep(unprotectedSecret, code, now);
        if (timeStep is null)
        {
            return false;
        }

        // A code that already opened something in its time step is a replay: refused.
        return await _useGuard.TryClaimTotpStepAsync(userId, timeStep.Value, cancellationToken);
    }

    private async Task<bool> TryVerifyRecoveryCodeAsync(
        Guid userId, string code, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var batch = await _context.MfaRecoveryBatches
            .Include(b => b.Codes)
            .Where(b => b.UserId == userId && b.InvalidatedAt == null)
            .OrderBy(b => b.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (batch is null)
        {
            return false;
        }

        var hash = _recoveryGenerator.Hash(code);
        if (!batch.Codes.Any(c => c.ConsumedAt is null && string.Equals(c.CodeHash, hash, StringComparison.Ordinal)))
        {
            return false;
        }

        // Two requests presenting the same code at once both see it unused: only the first claim goes on.
        if (!await _useGuard.TryClaimRecoveryCodeAsync(userId, hash, cancellationToken))
        {
            return false;
        }

        return batch.TryConsume(hash, now, userId);
    }
}