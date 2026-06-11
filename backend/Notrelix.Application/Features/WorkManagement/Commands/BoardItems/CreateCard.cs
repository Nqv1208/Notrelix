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

public record CreateCardCommand(Guid GroupId, string Title, double? Position = null) : IRequest<Result<Guid>>;

public class CreateCardCommandHandler : IRequestHandler<CreateCardCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;

    public CreateCardCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IWorkspacePermissionService permissions)
    {
        _context = context;
        _currentUser = currentUser;
        _permissions = permissions;
    }

    public async Task<Result<Guid>> Handle(CreateCardCommand request, CancellationToken cancellationToken)
    {
        var list = await _context.BoardGroups
            .FirstOrDefaultAsync(x => x.Id == request.GroupId, cancellationToken);

        if (list == null)
            throw new NotFoundException(nameof(BoardGroup), request.GroupId);

        await _permissions.EnsureCanEditBoardAsync(list.BoardId, _currentUser.UserId, cancellationToken);

        // Tính toán position (mặc định đặt ở cuối danh sách)
        var maxPosition = await _context.BoardItems
            .Where(x => x.GroupId == request.GroupId && !x.IsDeleted)
            .MaxAsync(x => (double?)x.Position, cancellationToken) ?? 0;

        var newPosition = request.Position ?? maxPosition + 65536.0; // Khoảng cách an toàn ban đầu

        var board = await _context.Boards
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == list.BoardId, cancellationToken);
        if (board == null)
            throw new NotFoundException(nameof(Board), list.BoardId);

        var card = BoardItem.Create(
            groupId: request.GroupId,
            boardId: list.BoardId,
            workspaceId: board.WorkspaceId,
            createdBy: _currentUser.UserId,
            title: request.Title,
            position: newPosition
        );

        _context.BoardItems.Add(card);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(card.Id);
    }
}
