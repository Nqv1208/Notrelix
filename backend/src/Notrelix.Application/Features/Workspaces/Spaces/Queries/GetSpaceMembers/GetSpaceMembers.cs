using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.Workspaces.Spaces.Services;

namespace Notrelix.Application.Features.Workspaces.Spaces.Queries.GetSpaceMembers;

/// <param name="SubjectType"><c>User</c> or <c>Team</c>.</param>
/// <param name="Role"><c>Admin</c>, <c>Member</c> or <c>Viewer</c>.</param>
/// <param name="DisplayName">The user's name or the Team's name.</param>
public record SpaceMemberDto(
    string SubjectType,
    Guid SubjectId,
    string Role,
    string DisplayName,
    string? AvatarUrl,
    DateTimeOffset AddedAt);

/// <summary>
/// The direct member entries (Users and Teams) of one Space the caller can see (WSP-024) as a cursor page in
/// subject id order (LD-06; default 50, at most 200). Pass <c>NextCursor</c> back as <c>Cursor</c> for the next page.
/// A Space the caller cannot see is NotFound. <c>Role</c> (<c>Admin</c>, <c>Member</c>, <c>Viewer</c>) and
/// <c>Kind</c> (<c>User</c>, <c>Team</c>) narrow the entries before the page is cut; they only ever remove rows,
/// so a filter never widens who can read the list.
/// </summary>
public record GetSpaceMembersQuery(
    Guid WorkspaceId,
    Guid SpaceId,
    string? Cursor = null,
    int? Limit = null,
    string? Role = null,
    string? Kind = null)
    : IQuery<Result<CursorPage<SpaceMemberDto>>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class GetSpaceMembersQueryHandler : IRequestHandler<GetSpaceMembersQuery, Result<CursorPage<SpaceMemberDto>>>
{
    private readonly ISpaceStore _spaces;
    private readonly IResourceVisibility _visibility;
    private readonly IActorLookupService _actors;
    private readonly ICurrentRequestContext _requestContext;

    public GetSpaceMembersQueryHandler(
        ISpaceStore spaces,
        IResourceVisibility visibility,
        IActorLookupService actors,
        ICurrentRequestContext requestContext)
    {
        _spaces = spaces;
        _visibility = visibility;
        _actors = actors;
        _requestContext = requestContext;
    }

    public async Task<Result<CursorPage<SpaceMemberDto>>> Handle(GetSpaceMembersQuery request, CancellationToken ct)
    {
        var visible = await _visibility.VisibleSpaceIdsAsync(request.WorkspaceId, _requestContext.UserId, ct);
        if (!visible.Contains(request.SpaceId))
            throw new NotFoundException(nameof(Space), request.SpaceId);

        var limit = request.Limit ?? SpaceIdCursor.DefaultLimit;
        Guid? after = SpaceIdCursor.TryDecode(request.Cursor, out var cursorId) ? cursorId : null;
        SpaceRole? role = Enum.TryParse<SpaceRole>(request.Role, ignoreCase: true, out var parsedRole) ? parsedRole : null;
        SpaceMemberSubjectType? kind = Enum.TryParse<SpaceMemberSubjectType>(request.Kind, ignoreCase: true, out var parsedKind) ? parsedKind : null;
        var rows = await _spaces.ListMemberPageAsync(request.WorkspaceId, request.SpaceId, after, limit + 1, role, kind, ct);

        var hasMore = rows.Count > limit;
        var page = rows.Take(limit).ToList();
        var userIds = page.Where(r => r.SubjectType == SpaceMemberSubjectType.User).Select(r => r.SubjectId).Distinct().ToList();
        var actors = (await _actors.FindManyAsync(userIds, ct)).ToDictionary(a => a.UserId);

        var items = page
            .Select(r =>
            {
                actors.TryGetValue(r.SubjectId, out var actor);
                var name = r.SubjectType == SpaceMemberSubjectType.Team
                    ? r.TeamName ?? "Unknown team"
                    : actor?.Name ?? "Unknown";
                return new SpaceMemberDto(
                    r.SubjectType.ToString(),
                    r.SubjectId,
                    r.Role.ToString(),
                    name,
                    r.SubjectType == SpaceMemberSubjectType.User ? actor?.AvatarUrl : null,
                    r.AddedAt);
            })
            .ToList();

        var nextCursor = hasMore ? SpaceIdCursor.Encode(page[^1].SubjectId) : null;
        return Result<CursorPage<SpaceMemberDto>>.Success(new CursorPage<SpaceMemberDto>(items, nextCursor));
    }
}
