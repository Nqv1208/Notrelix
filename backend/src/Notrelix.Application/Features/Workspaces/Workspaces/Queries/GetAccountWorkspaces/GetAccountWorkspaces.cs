using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Workspaces.DTOs;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Workspaces.Queries.GetAccountWorkspaces;

public record GetAccountWorkspacesQuery : IQuery<Result<List<WorkspaceDto>>>, IAccountRequest, IRequirePermission
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

        var workspaces = await _context.Workspaces
            .AsNoTracking()
            .Where(w => w.AccountId == accountId)
            .OrderBy(w => w.Name)
            .ToListAsync(ct);

        var workspaceIds = workspaces.Select(w => w.Id).ToList();
        var memberCounts = workspaceIds.Count == 0
            ? new Dictionary<Guid, int>()
            : await _context.WorkspaceMembers
                .AsNoTracking()
                .Where(m => workspaceIds.Contains(m.WorkspaceId))
                .GroupBy(m => m.WorkspaceId)
                .Select(g => new { WorkspaceId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.WorkspaceId, x => x.Count, ct);

        var result = workspaces.Select(w => new WorkspaceDto(
            w.Id, w.Name, w.Slug, w.Description, w.IsPersonal,
            "free", null, null,
            null, w.Status == WorkspaceStatus.Archived,
            memberCounts.GetValueOrDefault(w.Id),
            w.CreatedAt.DateTime,
            null
        )).ToList();

        return Result<List<WorkspaceDto>>.Success(result);
    }
}
