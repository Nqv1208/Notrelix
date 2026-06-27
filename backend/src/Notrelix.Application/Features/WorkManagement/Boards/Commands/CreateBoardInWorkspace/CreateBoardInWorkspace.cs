using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using BoardFieldEntity = global::Notrelix.Domain.WorkManagement.Fields.BoardField;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

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
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IWorkspaceAccessChecker _workspaceAccessChecker;

    public CreateBoardInWorkspaceCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        IWorkspaceAccessChecker workspaceAccessChecker)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _workspaceAccessChecker = workspaceAccessChecker;
    }

    public async Task<Result<Guid>> Handle(CreateBoardInWorkspaceCommand request, CancellationToken ct)
    {
        var workspaceCheck = await _workspaceAccessChecker.EnsureWorkspaceIsActiveAsync(request.WorkspaceId, ct);
        if (!workspaceCheck.Succeeded)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

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
