using Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.ArchiveBoardItem;

public record ArchiveBoardItemCommand(Guid BoardItemId) : ICommand<Result>, ITransactionalRequest;

public class ArchiveBoardItemCommandHandler : IRequestHandler<ArchiveBoardItemCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;
    private readonly IDateTimeProvider _timeProvider;

    public ArchiveBoardItemCommandHandler(
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

    public async Task<Result> Handle(ArchiveBoardItemCommand request, CancellationToken ct)
    {
        var card = await _context.BoardItems
            .FirstOrDefaultAsync(c => c.Id == request.BoardItemId, ct);
        if (card is null) throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        await _permissions.EnsureCanEditBoardAsync(card.BoardId, _currentUser.UserId, ct);

        var now = _timeProvider.UtcNow;
        card.SoftDelete(_currentUser.UserId, now);

        return Result.Success();
    }
}
