using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.WorkManagement.Public.Authorization;
using Notrelix.Application.Features.Collaboration.Watchers.Commands.FollowBoardItem;

namespace Notrelix.Application.Features.Collaboration.Watchers.Commands.UnfollowBoardItem;

/// <summary>The caller stops following an Item; not following it already is the same as having stopped.</summary>
[IdempotencyOperation("collaboration.watchers.unfollow-board-item.v1")]
public record UnfollowBoardItemCommand(Guid BoardItemId)
    : ICommand<Result>, IWriteRequest, IPersonalBoardRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create(FollowBoardItemCommand.BoardItemKind), BoardItemId);
}

public class UnfollowBoardItemCommandHandler : IRequestHandler<UnfollowBoardItemCommand, Result>
{
    private readonly ICollaborationDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _timeProvider;

    public UnfollowBoardItemCommandHandler(ICollaborationDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider timeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(UnfollowBoardItemCommand request, CancellationToken ct)
    {
        var userId = _requestContext.UserId;
        var kind = ResourceKind.Create(FollowBoardItemCommand.BoardItemKind);

        var watchers = await _context.ResourceWatchers
            .Where(w => w.UserId == userId && w.Target.Kind == kind && w.Target.ResourceId == request.BoardItemId)
            .ToListAsync(ct);
        foreach (var watcher in watchers)
        {
            watcher.Unwatch(userId, _timeProvider.UtcNow);
            _context.ResourceWatchers.Remove(watcher);
        }

        return Result.Success();
    }
}
