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
    private readonly IIdentityDbContext _context;
    private readonly ICurrentRequestContext _currentUser;

    public GetUserSessionsQueryHandler(
        IIdentityDbContext context,
        ICurrentRequestContext currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<SessionInfoDto>>> Handle(
        GetUserSessionsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var currentSessionId = _currentUser.SessionId;

        var sessions = await _context.Sessions
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);

        var dtos = sessions.Select(s => new SessionInfoDto(
            s.Id,
            s.Status,
            s.CreatedAt,
            s.ExpiresAt,
            s.RevokedAt,
            s.IpAddress,
            s.UserAgent,
            IsCurrent: currentSessionId is not null && s.Id == currentSessionId)).ToList();

        return Result<IReadOnlyList<SessionInfoDto>>.Success(dtos);
    }
}