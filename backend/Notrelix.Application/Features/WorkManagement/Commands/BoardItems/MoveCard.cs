using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Commands.Boards;
using global::Notrelix.Application.Features.WorkManagement.DTOs;
using global::Notrelix.Domain.Workspaces;
using global::Notrelix.Domain.WorkManagement.Items;

namespace Notrelix.Application.Features.WorkManagement.Commands;

public record MoveCardCommand(Guid BoardItemId, Guid GroupId, double Position) : IRequest<Result>;

public class MoveCardCommandHandler : IRequestHandler<MoveCardCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;

    public MoveCardCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IWorkspacePermissionService permissions)
    {
        _context = context;
        _currentUser = currentUser;
        _permissions = permissions;
    }

    public async Task<Result> Handle(MoveCardCommand request, CancellationToken cancellationToken)
    {
        var card = await _context.BoardItems
            .Include(c => c.Group) // Lấy thông tin list cũ để publish event
            .FirstOrDefaultAsync(x => x.Id == request.BoardItemId, cancellationToken);

        if (card == null)
            throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        var targetList = await _context.BoardGroups
            .FirstOrDefaultAsync(x => x.Id == request.GroupId, cancellationToken);

        if (targetList == null)
            throw new NotFoundException(nameof(BoardGroup), request.GroupId);

        if (card.Group.BoardId != targetList.BoardId)
            throw new BusinessRuleViolationException("CardBoardMismatch", "BoardItem can only be moved between groups on the same board.");

        await _permissions.EnsureCanEditBoardAsync(card.Group.BoardId, _currentUser.UserId, cancellationToken);

        // Cập nhật vị trí và danh sách
        card.MoveToGroup(request.GroupId, request.Position, _currentUser.UserId);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
