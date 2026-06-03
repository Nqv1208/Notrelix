using BoardEntity = global::Notrelix.Domain.Entities.Boards.Board;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Interfaces;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Boards.Commands.Boards;
using global::Notrelix.Application.Features.Boards.Commands.Cards.LinkPageToCard;
using global::Notrelix.Application.Features.Boards.Commands.Cards;
using global::Notrelix.Application.Features.Boards.DTOs;
using global::Notrelix.Domain.Common.Exceptions;
using global::Notrelix.Domain.Entities.Boards;
using global::Notrelix.Domain.Entities.Document;
using global::Notrelix.Domain.Entities.Workspaces;
using global::Notrelix.Domain.Enums;
using global::Notrelix.Domain.Events.Board;

namespace Notrelix.Application.Features.Boards.Commands.Cards.LinkPageToCard;

public record LinkPageToCardCommand(Guid CardId, Guid PageId) : IRequest<Result>;

public class LinkPageToCardCommandHandler : IRequestHandler<LinkPageToCardCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;

    public LinkPageToCardCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IWorkspacePermissionService permissions)
    {
        _context = context;
        _currentUser = currentUser;
        _permissions = permissions;
    }

    public async Task<Result> Handle(LinkPageToCardCommand request, CancellationToken cancellationToken)
    {
        var card = await _context.Cards
            .Include(c => c.List)
            .FirstOrDefaultAsync(x => x.Id == request.CardId, cancellationToken);

        if (card == null)
            throw new NotFoundException(nameof(Card), request.CardId);

        var page = await _context.Pages
            .FirstOrDefaultAsync(x => x.Id == request.PageId, cancellationToken);

        if (page == null)
            throw new NotFoundException(nameof(Page), request.PageId);

        var board = await _context.Boards
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == card.List.BoardId, cancellationToken);

        if (board == null)
            throw new NotFoundException(nameof(BoardEntity), card.List.BoardId);

        await _permissions.EnsureCanEditBoardAsync(board.Id, _currentUser.UserId, cancellationToken);

        if (board.WorkspaceId != page.WorkspaceId)
            throw new BusinessRuleViolationException("CardPageWorkspaceMismatch", "Card chỉ được link với page cùng workspace.");

        // Map link
        card.LinkPage(request.PageId, _currentUser.UserId);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
