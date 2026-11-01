using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Sessions.DTOs;

namespace Notrelix.Application.Features.Identity.Sessions.Queries.GetUserSessions;

public sealed record GetUserSessionsQuery
    : IQuery<Result<IReadOnlyList<SessionInfoDto>>>, IReadRequest,
      IGlobalRequest,
      IAuthenticatedRequest;

public sealed class GetUserSessionsQueryHandler
    : IRequestHandler<GetUserSessionsQuery, Result<IReadOnlyList<SessionInfoDto>>>
{
    /// <summary>How many ended (revoked or expired) sessions ride along with the live ones.</summary>
    public const int EndedSessionLimit = 20;

    private readonly IIdentityDbContext _context;
    private readonly ICurrentRequestContext _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetUserSessionsQueryHandler(
        IIdentityDbContext context,
        ICurrentRequestContext currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<IReadOnlyList<SessionInfoDto>>> Handle(
        GetUserSessionsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var currentSessionId = _currentUser.SessionId;
        var now = _dateTimeProvider.UtcNow;

        // Every live session, plus the most recent ended ones: the list never grows with
        // the number of refreshes (each refresh ends one session and starts the next).
        var live = await _context.Sessions
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.Status == SessionStatus.Active && s.ExpiresAt > now)
            .ToListAsync(cancellationToken);

        var ended = await _context.Sessions
            .AsNoTracking()
            .Where(s => s.UserId == userId && (s.Status != SessionStatus.Active || s.ExpiresAt <= now))
            .OrderByDescending(s => s.CreatedAt)
            .Take(EndedSessionLimit)
            .ToListAsync(cancellationToken);

        var dtos = live.Concat(ended)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new SessionInfoDto(
                s.Id,
                // A session past its end time that nothing closed reads as expired, not active.
                s.Status == SessionStatus.Active && s.ExpiresAt <= now ? SessionStatus.Expired : s.Status,
                s.CreatedAt,
                s.ExpiresAt,
                s.RevokedAt,
                s.IpAddress,
                s.UserAgent,
                IsCurrent: currentSessionId is not null && s.Id == currentSessionId))
            .ToList();

        return Result<IReadOnlyList<SessionInfoDto>>.Success(dtos);
    }
}
