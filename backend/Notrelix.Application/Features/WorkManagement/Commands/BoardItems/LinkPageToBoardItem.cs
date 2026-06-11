using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
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

public record LinkPageToBoardItemCommand(Guid BoardItemId, Guid PageId) : IRequest<Result>;

public class LinkPageToBoardItemCommandHandler : IRequestHandler<LinkPageToBoardItemCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;

    public LinkPageToBoardItemCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IWorkspacePermissionService permissions)
    {
        _context = context;
        _currentUser = currentUser;
        _permissions = permissions;
    }

    public async Task<Result> Handle(LinkPageToBoardItemCommand request, CancellationToken cancellationToken)
    {
        var card = await _context.BoardItems
            .Include(c => c.Group)
            .FirstOrDefaultAsync(x => x.Id == request.BoardItemId, cancellationToken);

        if (card == null)
            throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        var page = await _context.Pages
            .FirstOrDefaultAsync(x => x.Id == request.PageId, cancellationToken);

        if (page == null)
            throw new NotFoundException(nameof(Page), request.PageId);

        var board = await _context.Boards
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == card.Group.BoardId, cancellationToken);

        if (board == null)
            throw new NotFoundException(nameof(BoardEntity), card.Group.BoardId);

        await _permissions.EnsureCanEditBoardAsync(board.Id, _currentUser.UserId, cancellationToken);

        if (board.WorkspaceId != page.WorkspaceId)
            throw new BusinessRuleViolationException("CardPageWorkspaceMismatch", "BoardItem chỉ được link với page cùng workspace.");

        // Map link
        card.LinkPage(request.PageId);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
