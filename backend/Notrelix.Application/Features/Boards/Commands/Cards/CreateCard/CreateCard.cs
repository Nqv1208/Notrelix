using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Interfaces;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Boards.Commands.BoardLists;
using global::Notrelix.Application.Features.Boards.Commands.Boards;
using global::Notrelix.Application.Features.Boards.Commands.Cards.CreateCard;
using global::Notrelix.Application.Features.Boards.Commands.Cards;
using global::Notrelix.Application.Features.Boards.DTOs;
using global::Notrelix.Domain.Common.Exceptions;
using global::Notrelix.Domain.Entities.Boards;
using global::Notrelix.Domain.Entities.Workspaces;
using global::Notrelix.Domain.Enums;
using global::Notrelix.Domain.Events.Board;

namespace Notrelix.Application.Features.Boards.Commands.Cards.CreateCard;

public record CreateCardCommand(Guid ListId, string Title, double? Position = null) : IRequest<Result<Guid>>;

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
        var list = await _context.BoardLists
            .FirstOrDefaultAsync(x => x.Id == request.ListId, cancellationToken);

        if (list == null)
            throw new NotFoundException(nameof(BoardList), request.ListId);

        await _permissions.EnsureCanEditBoardAsync(list.BoardId, _currentUser.UserId, cancellationToken);

        // Tính toán position (mặc định đặt ở cuối danh sách)
        var maxPosition = await _context.Cards
            .Where(x => x.ListId == request.ListId && !x.IsDeleted)
            .MaxAsync(x => (double?)x.Position, cancellationToken) ?? 0;

        var newPosition = request.Position ?? maxPosition + 65536.0; // Khoảng cách an toàn ban đầu

        var card = Card.Create(
            listId: request.ListId,
            boardId: list.BoardId,
            createdBy: _currentUser.UserId,
            title: request.Title,
            position: newPosition
        );

        _context.Cards.Add(card);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(card.Id);
    }
}
