using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Mfa.DTOs;
using Notrelix.Domain.Identity.Mfa;

namespace Notrelix.Application.Features.Identity.Mfa.Services;

/// <summary>The single definition of a user's MFA status, shared by every read that reports it.</summary>
internal static class MfaStatusProjection
{
    public static async Task<MfaStatusDto> LoadAsync(
        IIdentityDbContext context, Guid userId, CancellationToken cancellationToken)
    {
        var methods = await context.UserMfaMethods
            .Where(m => m.UserId == userId)
            .ToListAsync(cancellationToken);

        var activeMethod = methods.FirstOrDefault(m => m.Status == MfaMethodStatus.Active);
        var isEnabled = activeMethod is not null;

        var primary = methods.FirstOrDefault(m => m.IsPrimary && m.Status == MfaMethodStatus.Active);
        var primaryMethod = primary?.Type.ToString() ?? activeMethod?.Type.ToString();

        // At most one batch is valid at a time; picking the newest in memory stays correct if
        // an older one was ever left un-invalidated.
        var activeBatch = (await context.MfaRecoveryBatches
                .Where(b => b.UserId == userId && b.InvalidatedAt == null)
                .Include(b => b.Codes)
                .ToListAsync(cancellationToken))
            .OrderByDescending(b => b.CreatedAt)
            .FirstOrDefault();

        var remaining = activeBatch?.Codes.Count(c => c.ConsumedAt == null) ?? 0;

        return new MfaStatusDto(
            IsEnabled: isEnabled,
            PrimaryMethod: primaryMethod,
            HasRecoveryCodes: remaining > 0,
            RecoveryCodesRemaining: remaining);
    }
}
