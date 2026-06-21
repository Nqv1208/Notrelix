using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItem;

public record UpdateBoardItemCommand(Guid BoardItemId, string? Title, string? DescriptionMd, string? Priority, string? Cover, DateTime? DueDate, DateTime? StartDate) : ICommand<Result>, ITransactionalRequest;

public class UpdateBoardItemCommandHandler : IRequestHandler<UpdateBoardItemCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;
    private readonly IDateTimeProvider _timeProvider;

    public UpdateBoardItemCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IWorkspacePermissionService permissions,
        IDateTimeProvider timeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _permissions = permissions;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(UpdateBoardItemCommand request, CancellationToken ct)
    {
        var card = await _context.BoardItems
            .FirstOrDefaultAsync(c => c.Id == request.BoardItemId && !c.IsDeleted, ct);
        if (card is null) throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        await _permissions.EnsureCanEditBoardAsync(card.BoardId, _currentUser.UserId, ct);

        var now = _timeProvider.UtcNow;

        if (request.Title is not null) card.Rename(request.Title, _currentUser.UserId, now);
        if (request.DueDate.HasValue || request.StartDate.HasValue)
            card.SetTimeline(
                request.StartDate.HasValue ? new DateTimeOffset(request.StartDate.Value, TimeSpan.Zero) : card.StartedAt,
                request.DueDate.HasValue ? new DateTimeOffset(request.DueDate.Value, TimeSpan.Zero) : card.DueAt,
                _currentUser.UserId,
                now);

        return Result.Success();
    }
}
