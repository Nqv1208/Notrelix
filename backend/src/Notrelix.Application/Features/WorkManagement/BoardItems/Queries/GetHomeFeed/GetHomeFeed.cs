using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Collaboration.Public.Notifications;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Queries.GetHomeFeed;

/// <summary>
/// What waits for the member on Home: the replies the member owes, as the unread mentions of Items they can
/// still read, newest first (at most <see cref="ReplyLimit"/>), and the counts the Home header shows. A mention
/// of an Item on a Board the caller can no longer read is not shown and not counted (COL-023: a historical
/// notification never keeps access alive). Mentions are counted over the newest <see cref="MentionScanLimit"/>
/// unread ones. Approvals and today's schedule have no source yet: no approval replies, no events, zero
/// pending approvals.
/// </summary>
public record GetHomeFeedQuery(Guid WorkspaceId)
    : IQuery<Result<HomeFeedDto>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    public const int ReplyLimit = 20;
    public const int MentionScanLimit = 100;

    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId);
}

public sealed record HomeFeedPersonDto(Guid Id, string Name);

public sealed record HomeReplyTargetDto(string Kind, Guid Id, Guid? BoardId, string? Key, string? Title);

/// <summary>A reply the member owes. <c>Text</c> is the quoted words when the source has an excerpt; a mention has none yet.</summary>
public sealed record HomeReplyDto(
    Guid Id,
    string Kind,
    HomeFeedPersonDto Actor,
    DateTimeOffset CreatedAt,
    string? Text,
    HomeReplyTargetDto Target);

public sealed record HomeCalendarEventDto(
    Guid Id,
    string Title,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string? Place,
    int? AttendeeCount);

public sealed record HomeFeedSummaryDto(int ApprovalsPending, int MentionsUnread);

public sealed record HomeFeedDto(
    IReadOnlyList<HomeReplyDto> Replies,
    IReadOnlyList<HomeCalendarEventDto> Events,
    HomeFeedSummaryDto Summary);

public class GetHomeFeedQueryHandler(
    IWorkManagementDbContext context,
    ICurrentRequestContext requestContext,
    IResourceVisibility visibility,
    INotificationInbox inbox,
    IActorLookupService actors) : IRequestHandler<GetHomeFeedQuery, Result<HomeFeedDto>>
{
    private const string BoardItemKind = "work-management.board-item";

    public async Task<Result<HomeFeedDto>> Handle(GetHomeFeedQuery request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;

        var mentions = (await inbox.RecentUnreadMentionsAsync(
                request.WorkspaceId, userId, GetHomeFeedQuery.MentionScanLimit, cancellationToken))
            .Where(m => m.ResourceKind == BoardItemKind && m.ResourceId is not null)
            .ToList();
        if (mentions.Count == 0)
        {
            return Result<HomeFeedDto>.Success(Empty());
        }

        var visibleBoardIds = await visibility.VisibleBoardIdsAsync(request.WorkspaceId, userId, cancellationToken);
        if (visibleBoardIds.Count == 0)
        {
            return Result<HomeFeedDto>.Success(Empty());
        }

        var itemIds = mentions.Select(m => m.ResourceId!.Value).Distinct().ToList();
        var items = await context.BoardItems
            .AsNoTracking()
            .Where(i => itemIds.Contains(i.Id)
                        && i.WorkspaceId == request.WorkspaceId
                        && i.DeletedAt == null
                        && visibleBoardIds.Contains(i.BoardId))
            .Select(i => new { i.Id, i.BoardId, i.ItemKey, i.Name })
            .ToDictionaryAsync(i => i.Id, cancellationToken);

        var readable = mentions.Where(m => items.ContainsKey(m.ResourceId!.Value)).ToList();
        var shown = readable.Take(GetHomeFeedQuery.ReplyLimit).ToList();

        var actorIds = shown.Select(m => m.ActorUserId).OfType<Guid>().Distinct().ToList();
        var people = (await actors.FindManyAsync(actorIds, cancellationToken))
            .ToDictionary(a => a.UserId, a => a.Name);

        var replies = shown
            .Where(m => m.ActorUserId is not null)
            .Select(m =>
            {
                var item = items[m.ResourceId!.Value];
                var actorId = m.ActorUserId!.Value;
                return new HomeReplyDto(
                    m.NotificationId,
                    "mention",
                    new HomeFeedPersonDto(actorId, people.GetValueOrDefault(actorId, string.Empty)),
                    m.CreatedAt,
                    null,
                    new HomeReplyTargetDto("item", item.Id, item.BoardId, item.ItemKey, item.Name));
            })
            .ToList();

        return Result<HomeFeedDto>.Success(new HomeFeedDto(replies, [], new HomeFeedSummaryDto(0, readable.Count)));
    }

    private static HomeFeedDto Empty() => new([], [], new HomeFeedSummaryDto(0, 0));
}

public class GetHomeFeedQueryValidator : AbstractValidator<GetHomeFeedQuery>
{
    public GetHomeFeedQueryValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
    }
}
