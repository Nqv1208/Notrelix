using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using BoardFieldEntity = global::Notrelix.Domain.WorkManagement.Fields.BoardField;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

using Notrelix.Domain.SharedKernel.Ordering;
namespace Notrelix.Application.Features.WorkManagement.Boards.Commands.CreateBoardInWorkspace;

[IdempotencyOperation("work-management.boards.create-board-in-workspace.v1")]
public record CreateBoardInWorkspaceCommand(
    Guid WorkspaceId,
    string Title,
    string? Description,
    string? Background,
    BoardVisibility? Visibility) : ICommand<Result<Guid>>, IWriteRequest, IRequirePermission, IAuthenticatedRequest, IWorkspaceRequest, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.CreateBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId);
}

public class CreateBoardInWorkspaceCommandHandler : IRequestHandler<CreateBoardInWorkspaceCommand, Result<Guid>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRealtimeChangeMapper<CreateBoardInWorkspaceCommand, Result<Guid>>? _realtime;
    private readonly IIntegrationEventCollector? _events;

    public CreateBoardInWorkspaceCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        IRealtimeChangeMapper<CreateBoardInWorkspaceCommand, Result<Guid>>? realtime = null,
        IIntegrationEventCollector? events = null)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _realtime = realtime;
        _events = events;
    }

    public async Task<Result<Guid>> Handle(CreateBoardInWorkspaceCommand request, CancellationToken ct)
    {
        // WorkspaceContextBehavior already resolved workspace access and set tenant context
        var accountId = _requestContext.RequireAccountId();
        var createdAt = _dateTimeProvider.UtcNow;
        var visibility = request.Visibility ?? BoardVisibility.Workspace;

        var board = BoardEntity.Create(accountId, request.WorkspaceId, _requestContext.UserId, request.Title, request.Description, createdAt, visibility);

        if (request.Background is not null) board.UpdateBackground(request.Background, _requestContext.UserId, createdAt);

        _context.Boards.Add(board);

        var defaultFields = new[]
        {
            BoardFieldEntity.Create(accountId, board.WorkspaceId, board.Id, "Title", FieldType.Text, FieldSettings.Empty(), FractionalIndex.Create("a0"), _requestContext.UserId, createdAt, isSystem: true),
            BoardFieldEntity.Create(accountId, board.WorkspaceId, board.Id, "Status", FieldType.Status, FieldSettings.Create(JsonValue.Create("{\"transitions\":{}}")!), FractionalIndex.Create("a1"), _requestContext.UserId, createdAt, isSystem: true),
            BoardFieldEntity.Create(accountId, board.WorkspaceId, board.Id, "Assignee", FieldType.Person, FieldSettings.Empty(), FractionalIndex.Create("a2"), _requestContext.UserId, createdAt, isSystem: true),
            BoardFieldEntity.Create(accountId, board.WorkspaceId, board.Id, "Due Date", FieldType.Date, FieldSettings.Empty(), FractionalIndex.Create("a3"), _requestContext.UserId, createdAt, isSystem: true),
        };
        _context.BoardFields.AddRange(defaultFields);

        var response = Result<Guid>.Success(board.Id);
        if (_realtime is not null && _events is not null)
            _events.Add(_realtime.Map(request, response, board.Version));
        return response;
    }
}

public sealed class CreateBoardInWorkspaceRealtimeMapper(IExecutionContextReader context, IDateTimeProvider time)
    : RealtimeChangeMapper<CreateBoardInWorkspaceCommand, Result<Guid>>(context, time)
{
    public override RealtimeResourceChangedV1 Map(CreateBoardInWorkspaceCommand request, Result<Guid> response, long streamVersion) =>
        Create("workspace", "Workspace", request.WorkspaceId, "CreateBoardInWorkspace", response, streamVersion);
}
