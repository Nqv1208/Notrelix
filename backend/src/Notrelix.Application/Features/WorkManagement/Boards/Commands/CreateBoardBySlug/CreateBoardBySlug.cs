using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using BoardFieldEntity = global::Notrelix.Domain.WorkManagement.Fields.BoardField;
using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Boards.Commands.CreateBoardBySlug;

public record CreateBoardBySlugCommand(string Slug, string Title, string? Description, string? Background, BoardVisibility? Visibility) : ICommand<Result<Guid>>, ITransactionalRequest;

public class CreateBoardBySlugCommandHandler : IRequestHandler<CreateBoardBySlugCommand, Result<Guid>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentTenantContext _tenant;
    private readonly IWorkspaceAccessResolver _workspaceAccess;

    public CreateBoardBySlugCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        ICurrentTenantContext tenant,
        IWorkspaceAccessResolver workspaceAccess)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _tenant = tenant;
        _workspaceAccess = workspaceAccess;
    }

    public async Task<Result<Guid>> Handle(CreateBoardBySlugCommand request, CancellationToken ct)
    {
        var workspace = await _workspaceAccess.ResolveBySlugAsync(request.Slug, ct);

        if (workspace is null) throw new NotFoundException(nameof(Workspace), request.Slug);

        var createdAt = _dateTimeProvider.UtcNow;
        var visibility = request.Visibility ?? BoardVisibility.Workspace;

        var board = BoardEntity.Create(_tenant.RequireAccountId(), workspace.Id, _currentUser.UserId, request.Title, request.Description, createdAt, visibility);

        if (request.Background is not null) board.UpdateBackground(request.Background, _currentUser.UserId, createdAt);

        _context.Boards.Add(board);

        var defaultFields = new[]
        {
            BoardFieldEntity.Create(_tenant.RequireAccountId(), board.WorkspaceId, board.Id, "Title", FieldType.Text, FieldSettings.Empty(), FractionalIndex.Create("a0"), _currentUser.UserId, createdAt, isSystem: true),
            BoardFieldEntity.Create(_tenant.RequireAccountId(), board.WorkspaceId, board.Id, "Status", FieldType.Status, FieldSettings.Empty(), FractionalIndex.Create("a1"), _currentUser.UserId, createdAt, isSystem: true),
            BoardFieldEntity.Create(_tenant.RequireAccountId(), board.WorkspaceId, board.Id, "Assignee", FieldType.Person, FieldSettings.Empty(), FractionalIndex.Create("a2"), _currentUser.UserId, createdAt, isSystem: true),
            BoardFieldEntity.Create(_tenant.RequireAccountId(), board.WorkspaceId, board.Id, "Due Date", FieldType.Date, FieldSettings.Empty(), FractionalIndex.Create("a3"), _currentUser.UserId, createdAt, isSystem: true),
        };
        _context.BoardFields.AddRange(defaultFields);

        return Result<Guid>.Success(board.Id);
    }
}
