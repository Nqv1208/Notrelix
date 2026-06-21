using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
namespace Notrelix.Application.Features.Collaboration.Notifications.Queries.GetUserNotifications;

public record NotificationDto(
    Guid Id,
    Guid WorkspaceId,
    string WorkspaceName,
    Guid UserId,
    string Type,
    string Content,
    bool IsRead,
    DateTime CreatedAt
);

public record GetUserNotificationsQuery : IQuery<Result<List<NotificationDto>>>;

public class GetUserNotificationsQueryHandler : IRequestHandler<GetUserNotificationsQuery, Result<List<NotificationDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetUserNotificationsQueryHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<List<NotificationDto>>> Handle(GetUserNotificationsQuery request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return Result<List<NotificationDto>>.Success(new List<NotificationDto>());
        }

        var notifications = await _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == _currentUser.UserId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .ToListAsync(ct);

        var result = new List<NotificationDto>();

        foreach (var n in notifications)
        {
            var workspace = await _context.Workspaces
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == n.WorkspaceId, ct);
            var workspaceName = workspace?.Name ?? "Workspace";

            result.Add(new NotificationDto(
                n.Id,
                n.WorkspaceId,
                workspaceName,
                n.UserId,
                n.Type.ToString(),
                n.Content,
                n.IsRead,
                n.CreatedAt.DateTime
            ));
        }

        return Result<List<NotificationDto>>.Success(result);
    }
}
