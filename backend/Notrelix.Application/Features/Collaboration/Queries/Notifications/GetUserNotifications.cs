using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
namespace Notrelix.Application.Features.Shared.Queries.Notifications;

public record NotificationDto(
    Guid Id,
    Guid WorkspaceId,
    string WorkspaceName,
    Guid UserId,
    Guid? ActorId,
    string ActorName,
    string Type,
    string Payload,
    bool IsRead,
    DateTime CreatedAt
);

public record GetUserNotificationsQuery : IRequest<Result<List<NotificationDto>>>;

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
            .Take(50) // Limit to top 50 notifications
            .ToListAsync(ct);

        var result = new List<NotificationDto>();

        foreach (var n in notifications)
        {
            var workspace = await _context.Workspaces
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == n.WorkspaceId, ct);
            var workspaceName = workspace?.Name ?? "Workspace";

            var actorName = "Ai đó";
            if (n.ActorId.HasValue)
            {
                var actor = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == n.ActorId.Value, ct);
                actorName = actor?.Name ?? actor?.Email?.Value ?? "Người dùng";
            }

            result.Add(new NotificationDto(
                n.Id,
                n.WorkspaceId,
                workspaceName,
                n.UserId,
                n.ActorId,
                actorName,
                n.Type,
                n.Payload,
                n.IsRead,
                n.CreatedAt
            ));
        }

        return Result<List<NotificationDto>>.Success(result);
    }
}
