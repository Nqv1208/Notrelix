using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Domain.Identity.Mfa;

namespace Notrelix.Application.Features.Identity.Mfa.Commands.DisableMfa;

public sealed record DisableMfaCommand
    : ICommand<Result>,
      ITransactionalRequest,
      IGlobalRequest,
      IAuthenticatedRequest;

public sealed class DisableMfaCommandHandler : IRequestHandler<DisableMfaCommand, Result>
{
    private readonly IIdentityDbContext _context;
    private readonly ICurrentRequestContext _currentUser;
    private readonly IJwtBlacklistService _jwtBlacklist;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<DisableMfaCommandHandler> _logger;

    private static readonly TimeSpan RevocationWatermarkTtl = TimeSpan.FromHours(24);

    public DisableMfaCommandHandler(
        IIdentityDbContext context,
        ICurrentRequestContext currentUser,
        IJwtBlacklistService jwtBlacklist,
        IDateTimeProvider dateTimeProvider,
        ILogger<DisableMfaCommandHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _jwtBlacklist = jwtBlacklist;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result> Handle(DisableMfaCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var activeMethods = await _context.UserMfaMethods
            .Where(m => m.UserId == userId && m.Status == MfaMethodStatus.Active)
            .ToListAsync(cancellationToken);

        if (activeMethods.Count == 0)
        {
            return Result.Success();
        }

        var now = _dateTimeProvider.UtcNow;

        foreach (var method in activeMethods)
        {
            method.Disable(now);
        }

        var activeBatches = await _context.MfaRecoveryBatches
            .Where(b => b.UserId == userId && b.InvalidatedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var batch in activeBatches)
        {
            batch.Invalidate(now, userId);
        }

        var settings = await _context.UserSecuritySettings
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

        settings?.DisableMfa(now);

        var activeSessions = await _context.Sessions
            .Where(s => s.UserId == userId && s.Status == SessionStatus.Active)
            .ToListAsync(cancellationToken);

        foreach (var session in activeSessions)
        {
            session.Revoke(now);
        }

        await _jwtBlacklist.RevokeUserBeforeAsync(userId, now, RevocationWatermarkTtl);

        _logger.LogWarning("MFA disabled for {UserId}; {SessionCount} sessions revoked", userId, activeSessions.Count);

        return Result.Success();
    }
}
