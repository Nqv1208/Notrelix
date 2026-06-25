using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using BoardFieldEntity = global::Notrelix.Domain.WorkManagement.Fields.BoardField;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Common.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.SharedKernel;
using global::Notrelix.Domain.WorkManagement.Fields;
using global::Notrelix.Domain.Workspaces;
using global::Notrelix.Domain.Workspaces.Workspaces;

namespace Notrelix.Application.Features.WorkManagement.Boards.Commands.CreateBoardInWorkspace;

public record CreateBoardInWorkspaceCommand(
    Guid WorkspaceId,
    string Title,
    string? Description,
    string? Background,
    BoardVisibility? Visibility) : ICommand<Result<Guid>>, ITransactionalRequest, IRequirePermission, IWorkspaceRequest, IRealtimeRequest
{
    public PermissionAction Action => PermissionAction.CreateBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Workspace, WorkspaceId);
    public RealtimeTopic Topic => new("workspace", "Workspace", WorkspaceId);
}

public class CreateBoardInWorkspaceCommandHandler : IRequestHandler<CreateBoardInWorkspaceCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateBoardInWorkspaceCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateBoardInWorkspaceCommand request, CancellationToken ct)
    {
        var workspaceExists = await _context.Workspaces
            .AsNoTracking()
            .AnyAsync(w => w.Id == request.WorkspaceId && w.Status == WorkspaceStatus.Active && !w.IsDeleted, ct);

        if (!workspaceExists) throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var createdAt = _dateTimeProvider.UtcNow;
        var visibility = request.Visibility ?? BoardVisibility.Workspace;

        var board = BoardEntity.Create(request.WorkspaceId, _currentUser.UserId, request.Title, request.Description, createdAt, visibility);

        if (request.Background is not null) board.UpdateBackground(request.Background, _currentUser.UserId, createdAt);

        _context.Boards.Add(board);

        var defaultFields = new[]
        {
            BoardFieldEntity.Create(board.WorkspaceId, board.Id, "Title", FieldType.Text, FieldSettings.Empty(), FractionalIndex.Create("a0"), _currentUser.UserId, createdAt, isSystem: true),
            BoardFieldEntity.Create(board.WorkspaceId, board.Id, "Status", FieldType.Status, FieldSettings.Empty(), FractionalIndex.Create("a1"), _currentUser.UserId, createdAt, isSystem: true),
            BoardFieldEntity.Create(board.WorkspaceId, board.Id, "Assignee", FieldType.Person, FieldSettings.Empty(), FractionalIndex.Create("a2"), _currentUser.UserId, createdAt, isSystem: true),
            BoardFieldEntity.Create(board.WorkspaceId, board.Id, "Due Date", FieldType.Date, FieldSettings.Empty(), FractionalIndex.Create("a3"), _currentUser.UserId, createdAt, isSystem: true),
        };
        _context.BoardFields.AddRange(defaultFields);

        return Result<Guid>.Success(board.Id);
    }
}
