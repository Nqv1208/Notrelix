using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.WorkManagement.Public.Authorization;
using Notrelix.Domain.Collaboration.Watchers;

namespace Notrelix.Application.Features.Collaboration.Watchers.Commands.FollowBoardItem;

/// <summary>
/// The caller follows an Item they can read: they are told when it changes status, gets a comment or is
/// assigned. Following twice is the same as following once.
/// </summary>
[IdempotencyOperation("collaboration.watchers.follow-board-item.v1")]
public record FollowBoardItemCommand(Guid BoardItemId)
    : ICommand<Result>, IWriteRequest, IPersonalBoardRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public const string BoardItemKind = "work-management.board-item";

    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create(BoardItemKind), BoardItemId);
}

public class FollowBoardItemCommandHandler : IRequestHandler<FollowBoardItemCommand, Result>
{
    private readonly ICollaborationDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _timeProvider;
    private readonly IWatcherWriteLock _watcherLock;

    public FollowBoardItemCommandHandler(
        ICollaborationDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider timeProvider,
        IWatcherWriteLock watcherLock)
    {
        _context = context;
        _requestContext = requestContext;
        _timeProvider = timeProvider;
        _watcherLock = watcherLock;
    }

    public async Task<Result> Handle(FollowBoardItemCommand request, CancellationToken ct)
    {
        var userId = _requestContext.UserId;
        var kind = ResourceKind.Create(FollowBoardItemCommand.BoardItemKind);

        // Concurrent follows of the same item serialize on this lock; the check below then sees the committed row,
        // so a racing duplicate is the same no-op as a repeat follow (UC-COL-H03).
        await _watcherLock.AcquireAsync(userId, ResourceRef.Create(kind, request.BoardItemId), ct);

        var already = await _context.ResourceWatchers
            .AnyAsync(w => w.UserId == userId && w.Target.Kind == kind && w.Target.ResourceId == request.BoardItemId, ct);
        if (already) return Result.Success();

        var workspaceId = _requestContext.RequireWorkspaceId();
        _context.ResourceWatchers.Add(ResourceWatcher.Create(
            _requestContext.RequireAccountId(),
            workspaceId,
            ResourceRef.Create(kind, request.BoardItemId, workspaceId),
            userId,
            userId,
            _timeProvider.UtcNow));
        return Result.Success();
    }
}
