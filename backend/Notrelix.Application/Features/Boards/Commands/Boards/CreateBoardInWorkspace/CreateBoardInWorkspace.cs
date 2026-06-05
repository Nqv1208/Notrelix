using BoardEntity = global::Notrelix.Domain.Entities.Boards.Board;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Interfaces;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Boards.Commands.BoardColumns;
using global::Notrelix.Application.Features.Boards.Commands.Boards.CreateBoardInWorkspace;
using global::Notrelix.Application.Features.Boards.Commands.Boards;
using global::Notrelix.Application.Features.Boards.DTOs;
using global::Notrelix.Domain.Common.Exceptions;
using global::Notrelix.Domain.Entities.Boards;
using global::Notrelix.Domain.Entities.Document;
using global::Notrelix.Domain.Entities.Identity;
using global::Notrelix.Domain.Entities.Shared;
using global::Notrelix.Domain.Entities.Workspaces;
using global::Notrelix.Domain.Enums;

namespace Notrelix.Application.Features.Boards.Commands.Boards.CreateBoardInWorkspace;

public record CreateBoardInWorkspaceCommand(Guid WorkspaceId, string Title, string? Description, string? Background, string? Visibility) : IRequest<Result<Guid>>;

public class CreateBoardInWorkspaceCommandHandler : IRequestHandler<CreateBoardInWorkspaceCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;

    public CreateBoardInWorkspaceCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IWorkspacePermissionService permissions)
    {
        _context = context;
        _currentUser = currentUser;
        _permissions = permissions;
    }

    public async Task<Result<Guid>> Handle(CreateBoardInWorkspaceCommand request, CancellationToken ct)
    {
        var workspaceExists = await _context.Workspaces
            .AsNoTracking()
            .AnyAsync(w => w.Id == request.WorkspaceId && !w.IsArchived, ct);

        if (!workspaceExists) throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        if (!await _permissions.CanEditWorkspaceAsync(request.WorkspaceId, _currentUser.UserId, ct))
            throw new ForbiddenException("Bạn không có quyền tạo board trong workspace này.");

        var visibility = request.Visibility is not null
            ? Enum.Parse<BoardVisibility>(request.Visibility, ignoreCase: true)
            : BoardVisibility.Workspace;

        var board = BoardEntity.Create(request.WorkspaceId, _currentUser.UserId, request.Title, request.Description, visibility);

        if (request.Background is not null) board.UpdateBackground(request.Background);

        _context.Boards.Add(board);
        _context.BoardColumns.AddRange(BoardColumn.CreateDefaults(board.Id));
        await _context.SaveChangesAsync(ct);

        return Result<Guid>.Success(board.Id);
    }
}
