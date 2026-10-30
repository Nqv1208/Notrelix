using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Security.Abstractions;
using Notrelix.Domain.Identity.Security;

namespace Notrelix.Application.Features.Identity.Sessions.Commands.RevokeSession;

public sealed record RevokeSessionCommand
    : ICommand<Result>,
      IWriteRequest,
      IGlobalRequest,
      IAuthenticatedRequest
{
    public required Guid SessionId { get; init; }
}

public sealed class RevokeSessionCommandHandler
    : IRequestHandler<RevokeSessionCommand, Result>
{
    private readonly IIdentityDbContext _context;
    private readonly ICurrentRequestContext _currentUser;
    private readonly IJwtBlacklistService _jwtBlacklist;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ISecurityActivityRecorder _activity;
    private readonly ILogger<RevokeSessionCommandHandler> _logger;

    private static readonly TimeSpan SessionRevocationMarkerTtl = TimeSpan.FromHours(24);

    public RevokeSessionCommandHandler(
        IIdentityDbContext context,
        ICurrentRequestContext currentUser,
        IJwtBlacklistService jwtBlacklist,
        IDateTimeProvider dateTimeProvider,
        ISecurityActivityRecorder activity,
        ILogger<RevokeSessionCommandHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _jwtBlacklist = jwtBlacklist;
        _dateTimeProvider = dateTimeProvider;
        _activity = activity;
        _logger = logger;
    }

    public async Task<Result> Handle(RevokeSessionCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var session = await _context.Sessions
            .FirstOrDefaultAsync(s => s.Id == request.SessionId && s.UserId == userId, cancellationToken);

        if (session is null)
        {
            return Result.Failure(new ApplicationError(
                "identity.session.not-found",
                "Session not found.",
                ApplicationErrorType.NotFound));
        }

        // Already ended (revoked or expired): nothing to change and no second activity line.
        if (session.Status != SessionStatus.Active)
        {
            return Result.Success();
        }

        var now = _dateTimeProvider.UtcNow;
        session.Revoke(now, SessionRevocationReasons.UserRequested);
        await _jwtBlacklist.RevokeSessionBeforeAsync(session.Id, now, SessionRevocationMarkerTtl);
        _activity.Record(userId, SecurityActivityType.SessionRevoked, now, session.Id);

        _logger.LogInformation("Session {SessionId} revoked by user {UserId}", session.Id, userId);

        return Result.Success();
    }
}