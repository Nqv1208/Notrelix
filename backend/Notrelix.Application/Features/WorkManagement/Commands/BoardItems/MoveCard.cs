using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Models;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Items;

namespace Notrelix.Application.Features.WorkManagement.Commands;

public record MoveCardCommand(Guid BoardItemId, Guid GroupId, double Position) : IRequest<Result>;

public class MoveCardCommandHandler : IRequestHandler<MoveCardCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;
    private readonly IDateTimeProvider _timeProvider;

    public MoveCardCommandHandler(
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

    public async Task<Result> Handle(MoveCardCommand request, CancellationToken cancellationToken)
    {
        var card = await _context.BoardItems
            .FirstOrDefaultAsync(x => x.Id == request.BoardItemId, cancellationToken);

        if (card == null)
            throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        var targetList = await _context.BoardGroups
            .FirstOrDefaultAsync(x => x.Id == request.GroupId, cancellationToken);

        if (targetList == null)
            throw new NotFoundException(nameof(BoardGroup), request.GroupId);

        if (card.BoardId != targetList.BoardId)
            throw new BusinessRuleViolationException("CardBoardMismatch", "BoardItem can only be moved between groups on the same board.");

        await _permissions.EnsureCanEditBoardAsync(card.BoardId, _currentUser.UserId, cancellationToken);

        var now = _timeProvider.UtcNow;
        var position = FractionalIndex.Create(request.Position.ToString(System.Globalization.CultureInfo.InvariantCulture));

        card.MoveToGroup(BoardGroupRef.From(targetList), position, _currentUser.UserId, now);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
