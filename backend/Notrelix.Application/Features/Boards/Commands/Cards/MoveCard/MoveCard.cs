using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Interfaces;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Boards.Commands.BoardLists;
using global::Notrelix.Application.Features.Boards.Commands.Boards;
using global::Notrelix.Application.Features.Boards.Commands.Cards.MoveCard;
using global::Notrelix.Application.Features.Boards.Commands.Cards;
using global::Notrelix.Application.Features.Boards.DTOs;
using global::Notrelix.Domain.Common.Exceptions;
using global::Notrelix.Domain.Entities.Boards;
using global::Notrelix.Domain.Entities.Workspaces;
using global::Notrelix.Domain.Enums;
using global::Notrelix.Domain.Events.Board;

namespace Notrelix.Application.Features.Boards.Commands.Cards.MoveCard;

public record MoveCardCommand(Guid CardId, Guid ListId, double Position) : IRequest<Result>;

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
        var card = await _context.Cards
            .Include(c => c.List) // Lấy thông tin list cũ để publish event
            .FirstOrDefaultAsync(x => x.Id == request.CardId, cancellationToken);

        if (card == null)
            throw new NotFoundException(nameof(Card), request.CardId);

        var targetList = await _context.BoardLists
            .FirstOrDefaultAsync(x => x.Id == request.ListId, cancellationToken);

        if (targetList == null)
            throw new NotFoundException(nameof(BoardList), request.ListId);

        if (card.List.BoardId != targetList.BoardId)
            throw new BusinessRuleViolationException("CardBoardMismatch", "Card can only be moved between groups on the same board.");

        await _permissions.EnsureCanEditBoardAsync(card.List.BoardId, _currentUser.UserId, cancellationToken);

        // Cập nhật vị trí và danh sách
        card.MoveToGroup(request.ListId, request.Position, _currentUser.UserId);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
