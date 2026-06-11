using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Commands.Boards;
using global::Notrelix.Application.Features.WorkManagement.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.WorkManagement.Commands;

public record CreateBoardGroupCommand(Guid BoardId, string Title, double? Position, string? Color = null) : IRequest<Result<Guid>>;

public class CreateBoardGroupCommandHandler : IRequestHandler<CreateBoardGroupCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;

    public CreateBoardGroupCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IWorkspacePermissionService permissions)
    {
        _context = context;
        _currentUser = currentUser;
        _permissions = permissions;
    }

    public async Task<Result<Guid>> Handle(CreateBoardGroupCommand request, CancellationToken ct)
    {
        var boardExists = await _context.Boards.AnyAsync(board => board.Id == request.BoardId && !board.IsArchived, ct);
        if (!boardExists) throw new NotFoundException(nameof(BoardEntity), request.BoardId);

        await _permissions.EnsureCanEditBoardAsync(request.BoardId, _currentUser.UserId, ct);

        var position = request.Position ?? await _context.BoardGroups
            .Where(l => l.BoardId == request.BoardId && !l.IsArchived)
            .MaxAsync(l => (double?)l.Position, ct) + 1 ?? 0;

        var list = BoardGroup.Create(request.BoardId, request.Title, position, request.Color);
        _context.BoardGroups.Add(list);
        await _context.SaveChangesAsync(ct);
        return Result<Guid>.Success(list.Id);
    }
}
