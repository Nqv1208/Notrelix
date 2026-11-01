using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Abstractions;

namespace Notrelix.Application.Features.Governance.Roles.Queries.ListWorkspaceRoles;

/// <summary>The built-in roles and the account's custom roles as the workspace Roles page lists them (UC-GOV-C06).</summary>
public sealed record ListWorkspaceRolesQuery(Guid WorkspaceId)
    : IQuery<Result<WorkspaceRoleListDto>>, IAuthenticatedRequest, IWorkspaceRequest, IReadRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageWorkspaceSettings;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
    ResourceRef? IRequirePermission.Resource => Resource;
}

public sealed class ListWorkspaceRolesQueryHandler : IRequestHandler<ListWorkspaceRolesQuery, Result<WorkspaceRoleListDto>>
{
    private static readonly string[] BuiltIn = ["Owner", "Admin", "Member", "Guest"];

    private readonly IGovernanceDbContext _context;
    private readonly ICurrentRequestContext _request;

    public ListWorkspaceRolesQueryHandler(IGovernanceDbContext context, ICurrentRequestContext request)
    {
        _context = context;
        _request = request;
    }

    public async Task<Result<WorkspaceRoleListDto>> Handle(ListWorkspaceRolesQuery query, CancellationToken cancellationToken)
    {
        var accountId = _request.RequireAccountId();
        var roles = await _context.CustomRoles.AsNoTracking().Include(r => r.Permissions)
            .Where(r => r.AccountId == accountId)
            .OrderBy(r => r.Name)
            .Take(100)
            .ToListAsync(cancellationToken);
        var counts = await _context.MemberRoleAssignments.AsNoTracking()
            .Where(a => a.WorkspaceId == query.WorkspaceId)
            .GroupBy(a => a.CustomRoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoleId, x => x.Count, cancellationToken);

        var rows = BuiltIn
            .Select(name => new WorkspaceRoleDto("BuiltIn", null, name, name, null, name, "Active", [], [], 0))
            .Concat(roles.Select(r => new WorkspaceRoleDto(
                "Custom", r.Id, r.Id.ToString("N"), r.Name, r.Description, r.BaseRole.ToString(), r.Status.ToString(),
                r.AddedActions.ToArray(), r.RemovedActions.ToArray(), counts.GetValueOrDefault(r.Id))))
            .ToArray();
        return Result<WorkspaceRoleListDto>.Success(new WorkspaceRoleListDto(rows));
    }
}
