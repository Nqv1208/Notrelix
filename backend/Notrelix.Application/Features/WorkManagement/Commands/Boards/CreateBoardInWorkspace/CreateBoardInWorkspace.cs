using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Common.Security;
using global::Notrelix.Application.Features.WorkManagement.Commands;
using global::Notrelix.Application.Features.WorkManagement.Commands.Boards.CreateBoardInWorkspace;
using global::Notrelix.Application.Features.WorkManagement.Commands.Boards;
using global::Notrelix.Application.Features.WorkManagement.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.WorkManagement.Commands.Boards.CreateBoardInWorkspace;

public record CreateBoardInWorkspaceCommand(
    Guid WorkspaceId,
    string Title,
    string? Description,
    string? Background,
    string? Visibility) : IRequest<Result<Guid>>, IAuthorizeableRequest
{
    ResourceType IAuthorizeableRequest.ResourceType => ResourceType.Workspace;
    Guid IAuthorizeableRequest.ResourceId => WorkspaceId;
    PermissionAction IAuthorizeableRequest.Action => PermissionAction.CreateBoard;
}

public class CreateBoardInWorkspaceCommandHandler : IRequestHandler<CreateBoardInWorkspaceCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public CreateBoardInWorkspaceCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(CreateBoardInWorkspaceCommand request, CancellationToken ct)
    {
        var workspaceExists = await _context.Workspaces
            .AsNoTracking()
            .AnyAsync(w => w.Id == request.WorkspaceId && !w.IsArchived, ct);

        if (!workspaceExists) throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var visibility = request.Visibility is not null
            ? Enum.Parse<BoardVisibility>(request.Visibility, ignoreCase: true)
            : BoardVisibility.Workspace;

        var board = BoardEntity.Create(request.WorkspaceId, _currentUser.UserId, request.Title, request.Description, visibility);

        if (request.Background is not null) board.UpdateBackground(request.Background);

        _context.Boards.Add(board);
        _context.BoardFields.AddRange(BoardField.CreateDefaults(board.WorkspaceId, board.Id));
        await _context.SaveChangesAsync(ct);

        return Result<Guid>.Success(board.Id);
    }
}
