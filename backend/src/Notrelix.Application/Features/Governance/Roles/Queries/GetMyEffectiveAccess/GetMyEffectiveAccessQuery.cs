using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Workspaces.Public.Members;

namespace Notrelix.Application.Features.Governance.Roles.Queries.GetMyEffectiveAccess;

/// <summary>
/// The caller's own effective role in the workspace: the role they act under and what a custom role adds or
/// removes, so a client hides what the person cannot do. Reading it needs only workspace membership.
/// </summary>
public sealed record GetMyEffectiveAccessQuery(Guid WorkspaceId)
    : IQuery<Result<MyEffectiveAccessDto>>, IAuthenticatedRequest, IWorkspaceRequest, IReadRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
    ResourceRef? IRequirePermission.Resource => Resource;
}

public sealed class GetMyEffectiveAccessQueryHandler : IRequestHandler<GetMyEffectiveAccessQuery, Result<MyEffectiveAccessDto>>
{
    private readonly IGovernanceDbContext _context;
    private readonly IWorkspaceMemberFacts _members;
    private readonly ICurrentRequestContext _request;

    public GetMyEffectiveAccessQueryHandler(IGovernanceDbContext context, IWorkspaceMemberFacts members, ICurrentRequestContext request)
    {
        _context = context;
        _members = members;
        _request = request;
    }

    public async Task<Result<MyEffectiveAccessDto>> Handle(GetMyEffectiveAccessQuery query, CancellationToken cancellationToken)
    {
        var userId = _request.UserId;
        var member = await _members.FindAsync(query.WorkspaceId, userId, cancellationToken)
            ?? throw new NotFoundException("WorkspaceMember", userId);

        var role = await (
            from a in _context.MemberRoleAssignments.AsNoTracking()
            join r in _context.CustomRoles.AsNoTracking().Include(r => r.Permissions) on a.CustomRoleId equals r.Id
            where a.WorkspaceId == query.WorkspaceId && a.MemberId == userId && r.Status == CustomRoleStatus.Active
            select r).FirstOrDefaultAsync(cancellationToken);

        // The Owner is never replaced; a role held by a person who is now Owner does not apply.
        return Result<MyEffectiveAccessDto>.Success(role is null || member.Role == "Owner"
            ? new MyEffectiveAccessDto(null, null, member.Role, [], [])
            : new MyEffectiveAccessDto(role.Id, role.Name, role.BaseRole.ToString(), role.AddedActions.ToArray(), role.RemovedActions.ToArray()));
    }
}
