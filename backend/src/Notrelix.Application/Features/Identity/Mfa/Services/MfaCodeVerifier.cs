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

    public MfaCodeVerifier(
        IIdentityDbContext context,
        IMfaTotpService totp,
        IMfaRecoveryCodeGenerator recoveryGenerator)
    {
        _context = context;
        _totp = totp;
        _recoveryGenerator = recoveryGenerator;
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

        return _totp.VerifyCode(unprotectedSecret, code, now);
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
        return batch.TryConsume(hash, now, userId);
    }
}