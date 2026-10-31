using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Governance.Authorization;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Governance.Roles;

namespace Notrelix.Application.Features.Governance.Roles.Queries;

/// <summary>The product-defined permission catalog (UC-GOV-C06). No tenant data: any signed-in person may read it.</summary>
public sealed record GetPermissionCatalogQuery : IQuery<Result<PermissionCatalogDto>>, IAuthenticatedRequest, IGlobalRequest, INoDataRequest;

public sealed class GetPermissionCatalogQueryHandler : IRequestHandler<GetPermissionCatalogQuery, Result<PermissionCatalogDto>>
{
    public Task<Result<PermissionCatalogDto>> Handle(GetPermissionCatalogQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(Result<PermissionCatalogDto>.Success(new PermissionCatalogDto(PermissionCatalog.Entries)));
}

/// <summary>The account's role definitions with the number of people holding each (at most 100 roles).</summary>
public sealed record ListCustomRolesQuery
    : IQuery<Result<CustomRoleListDto>>, IAuthenticatedRequest, IAccountRequest, IReadRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageAccount;
    public ResourceRef? Resource => null;
}

public sealed class ListCustomRolesQueryHandler : IRequestHandler<ListCustomRolesQuery, Result<CustomRoleListDto>>
{
    private readonly IGovernanceDbContext _context;
    private readonly ICurrentRequestContext _request;

    public ListCustomRolesQueryHandler(IGovernanceDbContext context, ICurrentRequestContext request)
    {
        _context = context;
        _request = request;
    }

    public async Task<Result<CustomRoleListDto>> Handle(ListCustomRolesQuery query, CancellationToken cancellationToken)
    {
        var accountId = _request.RequireAccountId();
        var roles = await _context.CustomRoles.AsNoTracking().Include(r => r.Permissions)
            .Where(r => r.AccountId == accountId)
            .OrderBy(r => r.Name)
            .Take(CreateLimit)
            .ToListAsync(cancellationToken);
        // Assignments carry a workspace; the account view counts across them (the account filter and RLS bound it).
        var counts = await _context.MemberRoleAssignments.AsNoTracking().IgnoreQueryFilters()
            .Where(a => a.AccountId == accountId)
            .GroupBy(a => a.CustomRoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoleId, x => x.Count, cancellationToken);

        return Result<CustomRoleListDto>.Success(new CustomRoleListDto(
            roles.Select(r => CustomRoleRules.ToDto(r, counts.GetValueOrDefault(r.Id))).ToArray()));
    }

    private const int CreateLimit = 100;
}

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
