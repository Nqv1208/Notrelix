using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Workspaces.DTOs;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Domain.Workspaces.Rules;

namespace Notrelix.Application.Features.Workspaces.Workspaces.Queries.GetAccountWorkspaces;

public record GetAccountWorkspacesQuery : IQuery<Result<List<WorkspaceDto>>>, IAuthenticatedRequest, IReadRequest, IAccountRequest, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef? IRequirePermission.Resource => null;
}

public class GetAccountWorkspacesQueryHandler : IRequestHandler<GetAccountWorkspacesQuery, Result<List<WorkspaceDto>>>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;

    public GetAccountWorkspacesQueryHandler(IWorkspaceDbContext context, ICurrentRequestContext requestContext)
    {
        _context = context;
        _requestContext = requestContext;
    }

    public async Task<Result<List<WorkspaceDto>>> Handle(GetAccountWorkspacesQuery request, CancellationToken ct)
    {
        var accountId = _requestContext.RequireAccountId();
        var userId = _requestContext.UserId;

        // Being an Account member does not show a workspace the person is not in (WSP-004): the subject is the
        // caller's own active membership. An archived workspace stays out except for the people who administer it
        // (the Domain's ActiveAdministrator rule), and a deleted one never appears. An Account request carries no
        // Workspace, so the EF tenant filter of the workspace-scoped sets would hide every member row; the filters
        // are lifted and each condition is explicit.
        var own = _context.WorkspaceMembers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(m => m.AccountId == accountId && m.UserId == userId && m.Status == WorkspaceMemberStatus.Active);
        var administered = own.Where(WorkspaceMemberRules.ActiveAdministrator).Select(m => m.WorkspaceId);

        var workspaces = await _context.Workspaces
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(w => w.AccountId == accountId && w.DeletedAt == null)
            .Where(w => own.Any(m => m.WorkspaceId == w.Id))
            .Where(w => w.Status == WorkspaceStatus.Active
                || (w.Status == WorkspaceStatus.Archived && administered.Contains(w.Id)))
            .OrderBy(w => w.Name)
            .ToListAsync(ct);

        var workspaceIds = workspaces.Select(w => w.Id).ToList();
        var memberCounts = workspaceIds.Count == 0
            ? new Dictionary<Guid, int>()
            : await _context.WorkspaceMembers
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(m => workspaceIds.Contains(m.WorkspaceId) && m.Status != WorkspaceMemberStatus.Removed)
                .GroupBy(m => m.WorkspaceId)
                .Select(g => new { WorkspaceId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.WorkspaceId, x => x.Count, ct);

        var result = workspaces.Select(w => new WorkspaceDto(
            w.Id, w.Name, w.Slug, w.Description, w.IsPersonal,
            "free", null, null,
            null, w.Status == WorkspaceStatus.Archived,
            memberCounts.GetValueOrDefault(w.Id),
            w.CreatedAt.UtcDateTime,
            null,
            w.Version,
            w.Settings.Size,
            w.Settings.Language
        )).ToList();

        return Result<List<WorkspaceDto>>.Success(result);
    }
}
