using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Collaboration.Public.Watchers;
using Notrelix.Application.Features.Collaboration.Watchers.Commands.FollowBoardItem;

namespace Notrelix.Application.Features.Collaboration.Watchers.Queries.GetBoardItemFollowing;

public sealed record BoardItemFollowerDto(Guid UserId, string Name);

/// <summary>Whether the caller follows the Item, and everyone who does.</summary>
public sealed record BoardItemFollowingDto(bool Following, IReadOnlyList<BoardItemFollowerDto> Followers);

public record GetBoardItemFollowingQuery(Guid BoardItemId)
    : IQuery<Result<BoardItemFollowingDto>>, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create(FollowBoardItemCommand.BoardItemKind), BoardItemId);
}

public class GetBoardItemFollowingQueryHandler : IRequestHandler<GetBoardItemFollowingQuery, Result<BoardItemFollowingDto>>
{
    private readonly IResourceFollowers _followers;
    private readonly IActorLookupService _actors;
    private readonly ICurrentRequestContext _requestContext;

    public GetBoardItemFollowingQueryHandler(IResourceFollowers followers, IActorLookupService actors, ICurrentRequestContext requestContext)
    {
        _followers = followers;
        _actors = actors;
        _requestContext = requestContext;
    }

    public async Task<Result<BoardItemFollowingDto>> Handle(GetBoardItemFollowingQuery request, CancellationToken ct)
    {
        var followerIds = await _followers.FollowerIdsAsync(FollowBoardItemCommand.BoardItemKind, request.BoardItemId, ct);
        var names = (await _actors.FindManyAsync(followerIds, ct)).ToDictionary(actor => actor.UserId, actor => actor.Name);
        var followers = followerIds
            .Select(id => new BoardItemFollowerDto(id, names.GetValueOrDefault(id, "")))
            .OrderBy(follower => follower.Name, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(follower => follower.UserId)
            .ToList();
        return Result<BoardItemFollowingDto>.Success(
            new BoardItemFollowingDto(followerIds.Contains(_requestContext.UserId), followers));
    }
}
