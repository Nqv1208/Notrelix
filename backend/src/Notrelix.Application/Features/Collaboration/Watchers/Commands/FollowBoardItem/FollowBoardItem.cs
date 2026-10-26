using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Domain.Collaboration.Watchers;

namespace Notrelix.Application.Features.Collaboration.Watchers.Commands.FollowBoardItem;

/// <summary>
/// The caller follows an Item they can read: they are told when it changes status, gets a comment or is
/// assigned. Following twice is the same as following once.
/// </summary>
[IdempotencyOperation("collaboration.watchers.follow-board-item.v1")]
public record FollowBoardItemCommand(Guid BoardItemId)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
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

    public FollowBoardItemCommandHandler(ICollaborationDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider timeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(FollowBoardItemCommand request, CancellationToken ct)
    {
        var userId = _requestContext.UserId;
        var kind = ResourceKind.Create(FollowBoardItemCommand.BoardItemKind);

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
