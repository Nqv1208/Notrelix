using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Abstractions;

namespace Notrelix.Application.Features.Identity.Sessions.Commands.RevokeOtherSessions;

/// <summary>
/// Revokes every active session of the current user except the session that
/// issued the request (identified by the sid claim). Fail-closed when the
/// current session cannot be bound.
/// </summary>
public sealed record RevokeOtherSessionsCommand
    : ICommand<Result>,
      ITransactionalRequest,
      IGlobalRequest,
      IAuthenticatedRequest;

public sealed class RevokeOtherSessionsCommandHandler
    : IRequestHandler<RevokeOtherSessionsCommand, Result>
{
    private readonly IIdentityDbContext _context;
    private readonly ICurrentRequestContext _currentUser;
    private readonly IJwtBlacklistService _jwtBlacklist;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<RevokeOtherSessionsCommandHandler> _logger;

    private static readonly TimeSpan SessionRevocationMarkerTtl = TimeSpan.FromHours(24);

    public RevokeOtherSessionsCommandHandler(
        IIdentityDbContext context,
        ICurrentRequestContext currentUser,
        IJwtBlacklistService jwtBlacklist,
        IDateTimeProvider dateTimeProvider,
        ILogger<RevokeOtherSessionsCommandHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _jwtBlacklist = jwtBlacklist;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result> Handle(RevokeOtherSessionsCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        if (_currentUser.SessionId is not { } currentSessionId)
        {
            return Result.Failure(new ApplicationError(
                "identity.session.not-bound",
                "Current session is not bound to this request. Refresh and retry.",
                ApplicationErrorType.Conflict));
        }

        var sessions = await _context.Sessions
            .Where(s =>
                s.UserId == userId &&
                s.Id != currentSessionId &&
                s.Status == SessionStatus.Active)
            .ToListAsync(cancellationToken);

        var now = _dateTimeProvider.UtcNow;

        foreach (var session in sessions)
        {
            session.Revoke(now);
            await _jwtBlacklist.RevokeSessionBeforeAsync(session.Id, now, SessionRevocationMarkerTtl);
        }

        _logger.LogInformation("Revoked {Count} other sessions for user {UserId}", sessions.Count, userId);

        return Result.Success();
    }
}