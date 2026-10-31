using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Boards.Services;

namespace Notrelix.Application.Features.WorkManagement.Boards.Commands.MoveBoardToSpace;

/// <summary>
/// Moves a Board into a Workspaces Space, or back to the Workspace root
/// (<paramref name="SpaceId"/> null). Authorized on the source Board
/// (<c>ManageBoard</c>, pipeline) and on the target (the create-a-Board right
/// there, <see cref="IBoardSpacePlacement"/>; GOV-028). Guarded by the Board's
/// expected version; the same Space is a no-op that does not re-authorize.
/// </summary>
[IdempotencyOperation("work-management.boards.move-board-to-space.v1")]
public record MoveBoardToSpaceCommand(
    Guid BoardId,
    Guid? SpaceId,
    long ExpectedVersion) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IExpectedVersionRequest, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public class MoveBoardToSpaceCommandHandler : IRequestHandler<MoveBoardToSpaceCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly IBoardSpacePlacement _placement;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IBoardRealtimeNotifier? _realtime;

    public MoveBoardToSpaceCommandHandler(
        IWorkManagementDbContext context,
        IBoardSpacePlacement placement,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        IBoardRealtimeNotifier? realtime = null)
    {
        _realtime = realtime;
        _context = context;
        _placement = placement;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(MoveBoardToSpaceCommand request, CancellationToken ct)
    {
        var response = await HandleCore(request, ct);
        if (_realtime is not null) await _realtime.NotifyAsync(request, response, ct);
        return response;
    }

    private async Task<Result> HandleCore(MoveBoardToSpaceCommand request, CancellationToken ct)
    {
        var board = await _context.Boards.FirstOrDefaultAsync(b => b.Id == request.BoardId, ct)
            ?? throw new NotFoundException(nameof(BoardEntity), request.BoardId);

        if (board.SpaceId == request.SpaceId)
            return Result.Success();

        await _placement.AuthorizeTargetAsync(board.WorkspaceId, request.SpaceId, ct);
        if (request.SpaceId is { } spaceId)
            await _placement.EnsureActiveTargetAsync(board.WorkspaceId, spaceId, ct);

        board.PlaceInSpace(request.SpaceId, _requestContext.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
