using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.Shared.Commands.Notifications;

public record MarkAllNotificationsAsReadCommand : IRequest<Result>;

public class MarkAllNotificationsAsReadCommandHandler : IRequestHandler<MarkAllNotificationsAsReadCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public MarkAllNotificationsAsReadCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(MarkAllNotificationsAsReadCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return Result.Success();
        }

        var unreadNotifications = await _context.Notifications
            .Where(n => n.UserId == _currentUser.UserId && !n.IsRead)
            .ToListAsync(ct);

        foreach (var n in unreadNotifications)
        {
            n.MarkAsRead();
        }

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
