using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Collaboration.Preferences.DTOs;

namespace Notrelix.Application.Features.Collaboration.Preferences.Queries.GetNotificationPreferences;

/// <summary>
/// The caller's own notification preference matrix (accepted 2026-10-23, owned by Collaboration; one per
/// account, applied in every Workspace, owner decision 2026-11-15). Any signed-in person may read it; the
/// subject is always the principal, so another person's matrix is never addressable. Defaults apply to every cell the
/// person never changed, with version 0.
/// </summary>
public record GetNotificationPreferencesQuery
    : IQuery<Result<NotificationPreferencesDto>>, IAuthenticatedRequest, IReadRequest, IGlobalRequest;

public class GetNotificationPreferencesQueryHandler : IRequestHandler<GetNotificationPreferencesQuery, Result<NotificationPreferencesDto>>
{
    private readonly ICollaborationDbContext _context;
    private readonly ICurrentRequestContext _requestContext;

    public GetNotificationPreferencesQueryHandler(ICollaborationDbContext context, ICurrentRequestContext requestContext)
    {
        _context = context;
        _requestContext = requestContext;
    }

    public async Task<Result<NotificationPreferencesDto>> Handle(GetNotificationPreferencesQuery request, CancellationToken ct)
    {
        var userId = _requestContext.UserId;
        var set = await _context.NotificationPreferenceSets
            .AsNoTracking()
            .Include(s => s.Entries)
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

        return Result<NotificationPreferencesDto>.Success(NotificationPreferencesDto.From(set));
    }
}
