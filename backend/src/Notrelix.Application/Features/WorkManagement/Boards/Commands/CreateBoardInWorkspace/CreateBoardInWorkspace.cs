using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using BoardFieldEntity = global::Notrelix.Domain.WorkManagement.Fields.BoardField;
using BoardGroupEntity = global::Notrelix.Domain.WorkManagement.BoardGroups.BoardGroup;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.Identity.Public.Queries;
using Notrelix.Application.Features.WorkManagement.Boards.Services;

using Notrelix.Domain.SharedKernel.Ordering;
namespace Notrelix.Application.Features.WorkManagement.Boards.Commands.CreateBoardInWorkspace;

[IdempotencyOperation("work-management.boards.create-board-in-workspace.v1")]
public record CreateBoardInWorkspaceCommand(
    Guid WorkspaceId,
    string Title,
    string? Description,
    string? Background,
    BoardVisibility? Visibility,
    Guid? SpaceId = null) : ICommand<Result<Guid>>, IWriteRequest, IRequirePermission, IAuthenticatedRequest, IWorkspaceRequest, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.CreateBoard;

    // GOV-028: creating in a Space is authorized on that Space (Editor level);
    // at the Workspace root it keeps the Workspace-level CreateBoard decision.
    public ResourceRef Resource => SpaceId is { } spaceId
        ? ResourceRef.Create(ResourceKind.Create("workspaces.space"), spaceId, WorkspaceId)
        : ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId);
}

public class CreateBoardInWorkspaceCommandHandler : IRequestHandler<CreateBoardInWorkspaceCommand, Result<Guid>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly IBoardSpacePlacement _placement;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IIdentityUserFacts _identity;
    private readonly IRealtimeChangeMapper<CreateBoardInWorkspaceCommand, Result<Guid>>? _realtime;
    private readonly IIntegrationEventCollector? _events;

    public CreateBoardInWorkspaceCommandHandler(
        IWorkManagementDbContext context,
        IBoardSpacePlacement placement,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        IIdentityUserFacts identity,
        IRealtimeChangeMapper<CreateBoardInWorkspaceCommand, Result<Guid>>? realtime = null,
        IIntegrationEventCollector? events = null)
    {
        _context = context;
        _placement = placement;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _identity = identity;
        _realtime = realtime;
        _events = events;
    }

    public async Task<Result<Guid>> Handle(CreateBoardInWorkspaceCommand request, CancellationToken ct)
    {
        // WorkspaceContextBehavior already resolved workspace access and set tenant context
        var accountId = _requestContext.RequireAccountId();
        var createdAt = _dateTimeProvider.UtcNow;
        var visibility = request.Visibility ?? BoardVisibility.Workspace;

        if (request.SpaceId is { } spaceId)
            await _placement.EnsureActiveTargetAsync(request.WorkspaceId, spaceId, ct);

        var board = BoardEntity.Create(accountId, request.WorkspaceId, _requestContext.UserId, request.Title, request.Description, createdAt, visibility, spaceId: request.SpaceId);

        if (request.Background is not null) board.UpdateBackground(request.Background, _requestContext.UserId, createdAt);

        _context.Boards.Add(board);

        var defaultFields = new[]
        {
            BoardFieldEntity.Create(accountId, board.WorkspaceId, board.Id, "Title", FieldType.Text, FieldSettings.Empty(), FractionalIndex.Create("a0"), _requestContext.UserId, createdAt, isSystem: true),
            BoardFieldEntity.Create(accountId, board.WorkspaceId, board.Id, "Status", FieldType.Status, FieldSettings.Create(JsonValue.Create("{\"transitions\":{}}")!), FractionalIndex.Create("a1"), _requestContext.UserId, createdAt, isSystem: true),
            BoardFieldEntity.Create(accountId, board.WorkspaceId, board.Id, "Assignee", FieldType.Person, FieldSettings.Empty(), FractionalIndex.Create("a2"), _requestContext.UserId, createdAt, isSystem: true),
            BoardFieldEntity.Create(accountId, board.WorkspaceId, board.Id, "Due Date", FieldType.Date, FieldSettings.Empty(), FractionalIndex.Create("a3"), _requestContext.UserId, createdAt, isSystem: true),
        };
        // A new board starts with the default work statuses, named in the creator's profile language.
        var locale = await _identity.FindLocaleAsync(_requestContext.UserId, ct);
        defaultFields.Single(f => f.Type == FieldType.Status).AddDefaultStatusOptions(_requestContext.UserId, createdAt, locale);
        _context.BoardFields.AddRange(defaultFields);

        // A new board also starts with one group per default status, so an item can be added at once
        // (an Item always belongs to a group).
        var statuses = DefaultWorkStatuses.For(locale);
        var groupPositions = FractionalIndexGenerator.GenerateNKeysBetween(lower: null, upper: null, count: statuses.Count);
        for (var i = 0; i < statuses.Count; i++)
        {
            _context.BoardGroups.Add(BoardGroupEntity.Create(
                accountId, board.WorkspaceId, board.Id, statuses[i].Name, Color.Create(statuses[i].Color), groupPositions[i], _requestContext.UserId, createdAt));
        }

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
