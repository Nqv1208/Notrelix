using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Public.Access;
using Notrelix.Domain.Identity.Tokens;

namespace Notrelix.Application.Features.Identity.Users.Services;

/// <summary>
/// Producer-owned implementation of <see cref="IIdentityUserAccess"/>. It runs inside the caller's request
/// transaction (the pipeline owns the commit), so the status and every revocation commit together.
/// </summary>
public sealed class IdentityUserAccessProvider : IIdentityUserAccess
{
    // Longer than any access token, the same lifetime the password-reset watermark uses.
    private static readonly TimeSpan RevocationWatermarkTtl = TimeSpan.FromHours(24);

    private readonly IIdentityDbContext _context;
    private readonly IJwtBlacklistService _jwtBlacklist;
    private readonly IDateTimeProvider _dateTimeProvider;

    public IdentityUserAccessProvider(
        IIdentityDbContext context, IJwtBlacklistService jwtBlacklist, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _jwtBlacklist = jwtBlacklist;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<bool> EndAccessAsync(
        Guid userId, Guid endedBy, UserAccessEnd kind, string? reason, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            return false;

        var now = _dateTimeProvider.UtcNow;
        if (kind == UserAccessEnd.Suspended)
            user.Suspend(endedBy, now, reason);
        else
            user.Deactivate(endedBy, now, reason);

        var sessions = await _context.Sessions
            .Where(s => s.UserId == userId && s.Status == SessionStatus.Active)
            .ToListAsync(cancellationToken);
        foreach (var session in sessions)
            session.Revoke(now, SessionRevocationReasons.UserDeactivated);

        var tokens = await _context.ApiTokens
            .Where(t => t.UserId == userId && t.Status == ApiTokenStatus.Active)
            .ToListAsync(cancellationToken);
        foreach (var token in tokens)
            token.Revoke(endedBy, now, ApiTokenRevokedReason.UserDisabled);

        // Access tokens and realtime connections already issued stay valid until they expire unless the
        // user watermark rejects them.
        await _jwtBlacklist.RevokeUserBeforeAsync(userId, now, RevocationWatermarkTtl);
        return true;
    }
}
