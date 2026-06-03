using BoardEntity = global::Notrelix.Domain.Entities.Boards.Board;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Interfaces;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Boards.Commands.BoardLists.CreateList;
using global::Notrelix.Application.Features.Boards.Commands.BoardLists;
using global::Notrelix.Application.Features.Boards.Commands.Boards;
using global::Notrelix.Application.Features.Boards.DTOs;
using global::Notrelix.Domain.Common.Exceptions;
using global::Notrelix.Domain.Entities.Boards;
using global::Notrelix.Domain.Entities.Document;
using global::Notrelix.Domain.Entities.Identity;
using global::Notrelix.Domain.Entities.Shared;
using global::Notrelix.Domain.Entities.Workspaces;
using global::Notrelix.Domain.Enums;

namespace Notrelix.Application.Features.Boards.Commands.BoardLists.CreateList;

public record CreateListCommand(Guid BoardId, string Title, double? Position, string? Color = null) : IRequest<Result<Guid>>;

public class CreateListCommandHandler : IRequestHandler<CreateListCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;

    public CreateListCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IWorkspacePermissionService permissions)
    {
        _context = context;
        _currentUser = currentUser;
        _permissions = permissions;
    }

    public async Task<Result<Guid>> Handle(CreateListCommand request, CancellationToken ct)
    {
        var boardExists = await _context.Boards.AnyAsync(board => board.Id == request.BoardId && !board.IsArchived, ct);
        if (!boardExists) throw new NotFoundException(nameof(BoardEntity), request.BoardId);

        await _permissions.EnsureCanEditBoardAsync(request.BoardId, _currentUser.UserId, ct);

        var position = request.Position ?? await _context.BoardLists
            .Where(l => l.BoardId == request.BoardId && !l.IsArchived)
            .MaxAsync(l => (double?)l.Position, ct) + 1 ?? 0;

        var list = BoardList.Create(request.BoardId, request.Title, position, request.Color);
        _context.BoardLists.Add(list);
        await _context.SaveChangesAsync(ct);
        return Result<Guid>.Success(list.Id);
    }
}
