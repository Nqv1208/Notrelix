using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
namespace Notrelix.Application.Features.Shared.Commands.Notifications;

public record MarkNotificationAsReadCommand(Guid NotificationId) : IRequest<Result>;

public class MarkNotificationAsReadCommandHandler : IRequestHandler<MarkNotificationAsReadCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public MarkNotificationAsReadCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(MarkNotificationAsReadCommand request, CancellationToken ct)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == request.NotificationId && n.UserId == _currentUser.UserId, ct);

        if (notification is null)
            throw new NotFoundException(nameof(Notification), request.NotificationId);

        notification.MarkAsRead();
        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }
}
