using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Abstractions;

namespace Notrelix.Application.Features.Identity.Sessions.Commands.RevokeSession;

public sealed record RevokeSessionCommand
    : ICommand<Result>,
      ITransactionalRequest,
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
    private readonly ILogger<RevokeSessionCommandHandler> _logger;

    private static readonly TimeSpan SessionRevocationMarkerTtl = TimeSpan.FromHours(24);

    public RevokeSessionCommandHandler(
        IIdentityDbContext context,
        ICurrentRequestContext currentUser,
        IJwtBlacklistService jwtBlacklist,
        IDateTimeProvider dateTimeProvider,
        ILogger<RevokeSessionCommandHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _jwtBlacklist = jwtBlacklist;
        _dateTimeProvider = dateTimeProvider;
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

        if (session.Status == SessionStatus.Revoked)
        {
            return Result.Success();
        }

        var now = _dateTimeProvider.UtcNow;
        session.Revoke(now);
        await _jwtBlacklist.RevokeSessionBeforeAsync(session.Id, now, SessionRevocationMarkerTtl);

        _logger.LogInformation("Session {SessionId} revoked by user {UserId}", session.Id, userId);

        return Result.Success();
    }
}