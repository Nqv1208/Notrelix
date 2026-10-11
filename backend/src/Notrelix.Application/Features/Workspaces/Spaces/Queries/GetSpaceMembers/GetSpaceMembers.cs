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
/// The direct member entries (Users and Teams) of one Space the caller can see
/// (WSP-024). A Space the caller cannot see is NotFound.
/// </summary>
public record GetSpaceMembersQuery(Guid WorkspaceId, Guid SpaceId)
    : IQuery<Result<List<SpaceMemberDto>>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class GetSpaceMembersQueryHandler : IRequestHandler<GetSpaceMembersQuery, Result<List<SpaceMemberDto>>>
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

    public async Task<Result<List<SpaceMemberDto>>> Handle(GetSpaceMembersQuery request, CancellationToken ct)
    {
        var visible = await _visibility.VisibleSpaceIdsAsync(request.WorkspaceId, _requestContext.UserId, ct);
        if (!visible.Contains(request.SpaceId))
            throw new NotFoundException(nameof(Space), request.SpaceId);

        var rows = await _spaces.ListMembersAsync(request.WorkspaceId, request.SpaceId, ct);
        var userIds = rows.Where(r => r.SubjectType == SpaceMemberSubjectType.User).Select(r => r.SubjectId).Distinct().ToList();
        var actors = (await _actors.FindManyAsync(userIds, ct)).ToDictionary(a => a.UserId);

        var result = rows
            .OrderByDescending(r => r.Role)
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

        return Result<List<SpaceMemberDto>>.Success(result);
    }
}
