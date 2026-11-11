using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.Mentions.DTOs;
using Notrelix.Application.Features.Chat.Mentions.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Rules;
using Notrelix.Domain.Chat.Spaces;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Features.Chat.Mentions.Queries.ListMentionables;

/// <summary>
/// The mention picker of a space (UC-CHT-G01, G02, G03). Only an active member of the space composes there; anyone else gets the
/// same NotFound as a missing space. The people are the active members first, then, for a caller who is not a Guest, the active
/// colleagues of the Workspace who are not in the space, marked InSpace false so the composer can warn before the send (COL-030).
/// A Guest sees the people of the space only: no Workspace directory. The answer also says whether the caller may broadcast and the
/// member count above which a broadcast needs confirmation. People are ids; the client resolves their names.
/// </summary>
public record ListMentionablesQuery(Guid WorkspaceId, Guid SpaceId)
    : IQuery<Result<MentionablesDto>>, IReadRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    /// <summary>The most people one answer lists. Members of the space come first.</summary>
    public const int PeopleMax = 200;

    public PermissionAction Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class ListMentionablesQueryHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    IBroadcastAuthorizer broadcast) : IRequestHandler<ListMentionablesQuery, Result<MentionablesDto>>
{
    public async Task<Result<MentionablesDto>> Handle(ListMentionablesQuery request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var caller = await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);
        var space = await ChannelAccess.LoadLiveAsync(db, request.WorkspaceId, request.SpaceId, cancellationToken);
        if (!space.IsActiveMember(userId))
            throw new NotFoundException(nameof(Space), request.SpaceId);

        var isGuest = string.Equals(caller.Role, ChannelAccess.GuestRole, StringComparison.Ordinal);
        var inSpace = space.Memberships.Where(m => m.IsActive).Select(m => m.UserId).OrderBy(id => id).ToList();
        var people = inSpace.Select(id => new MentionableDto(id, InSpace: true)).ToList();

        // A conversation lists its participants only: the Workspace directory is a channel composer feature (COL-030).
        if (!isGuest && !space.IsConversation)
        {
            var inSpaceSet = inSpace.ToHashSet();
            var colleagues = await members.ListActiveColleagueIdsAsync(request.WorkspaceId, cancellationToken);
            people.AddRange(colleagues.Where(id => !inSpaceSet.Contains(id)).OrderBy(id => id).Select(id => new MentionableDto(id, InSpace: false)));
        }

        var canBroadcast = !isGuest && inSpace.Count > 1 && space.Kind != SpaceKind.Direct && await broadcast.CanBroadcastAsync(request.WorkspaceId, cancellationToken);
        return Result<MentionablesDto>.Success(new MentionablesDto(
            people.Take(ListMentionablesQuery.PeopleMax).ToList(), canBroadcast, ChatLimits.BroadcastConfirmThreshold, inSpace.Count));
    }
}

public class ListMentionablesQueryValidator : AbstractValidator<ListMentionablesQuery>
{
    public ListMentionablesQueryValidator()
    {
        RuleFor(x => x.SpaceId).NotEmpty();
    }
}
