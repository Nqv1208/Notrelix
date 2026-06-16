using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Models;
using Notrelix.Domain.WorkManagement.Items;

namespace Notrelix.Application.Features.WorkManagement.Commands;

public record CreateCardCommand(Guid GroupId, string Title, double? Position = null) : IRequest<Result<Guid>>;

public class CreateCardCommandHandler : IRequestHandler<CreateCardCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;
    private readonly IDateTimeProvider _timeProvider;

    public CreateCardCommandHandler(
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

    public async Task<Result<Guid>> Handle(CreateCardCommand request, CancellationToken cancellationToken)
    {
        var list = await _context.BoardGroups
            .FirstOrDefaultAsync(x => x.Id == request.GroupId, cancellationToken);

        if (list == null)
            throw new NotFoundException(nameof(BoardGroup), request.GroupId);

        await _permissions.EnsureCanEditBoardAsync(list.BoardId, _currentUser.UserId, cancellationToken);

        var lastItem = await _context.BoardItems
            .Where(x => x.GroupId == request.GroupId && !x.IsDeleted)
            .OrderByDescending(x => x.Position)
            .FirstOrDefaultAsync(cancellationToken);

        var position = request.Position.HasValue
            ? FractionalIndex.Create(request.Position.Value.ToString(System.Globalization.CultureInfo.InvariantCulture))
            : lastItem != null
                ? FractionalIndex.Create(lastItem.Position.Value + "1")
                : FractionalIndex.Initial();

        var board = await _context.Boards
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == list.BoardId, cancellationToken);
        if (board == null)
            throw new NotFoundException(nameof(Board), list.BoardId);

        var now = _timeProvider.UtcNow;

        var card = BoardItem.Create(
            board.WorkspaceId,
            list.BoardId,
            request.GroupId,
            request.Title,
            position,
            _currentUser.UserId,
            now);

        _context.BoardItems.Add(card);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(card.Id);
    }
}
