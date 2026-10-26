using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Collaboration.Notifications.DTOs;
using Notrelix.Application.Features.Collaboration.Public.Notifications;

namespace Notrelix.Application.Features.Collaboration.Notifications.Queries.GetInboxSummary;

/// <summary>
/// The caller's own unread notification counts per Workspace and in total: the badge numbers of the
/// workspace rail and the sidebar. A person-level read with no Workspace selected; only the caller's own
/// notifications are counted, and only in Workspaces where the caller is still an active member.
/// </summary>
public record GetInboxSummaryQuery : IQuery<Result<InboxSummaryDto>>, IAuthenticatedRequest, IReadRequest, IGlobalRequest;

public class GetInboxSummaryQueryHandler(
    INotificationInbox inbox,
    IWorkspaceAccessResolver workspaceAccess,
    ICurrentRequestContext requestContext) : IRequestHandler<GetInboxSummaryQuery, Result<InboxSummaryDto>>
{
    public async Task<Result<InboxSummaryDto>> Handle(GetInboxSummaryQuery request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var counts = await inbox.UnreadByWorkspaceAsync(userId, cancellationToken);

        var perWorkspace = new List<InboxWorkspaceUnreadDto>();
        foreach (var count in counts.Where(c => c.Unread > 0).OrderByDescending(c => c.Unread).ThenBy(c => c.WorkspaceId))
        {
            if (await IsActiveMemberAsync(count.WorkspaceId, userId, cancellationToken))
            {
                perWorkspace.Add(new InboxWorkspaceUnreadDto(count.WorkspaceId, count.Unread));
            }
        }

        return Result<InboxSummaryDto>.Success(new InboxSummaryDto(perWorkspace, perWorkspace.Sum(c => c.Unread)));
    }

    private async Task<bool> IsActiveMemberAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            var access = await workspaceAccess.ResolveAsync(workspaceId, userId, cancellationToken);
            return access.CanAccess && access.IsWorkspaceActive;
        }
        catch (NotFoundException)
        {
            return false;
        }
    }
}
