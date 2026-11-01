using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Abstractions;

namespace Notrelix.Application.Features.Governance.Roles.Queries.ListCustomRoles;

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
