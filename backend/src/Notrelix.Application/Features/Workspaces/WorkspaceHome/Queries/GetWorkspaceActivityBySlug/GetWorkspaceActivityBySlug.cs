using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Models;
namespace Notrelix.Application.Features.Workspaces.WorkspaceHome.Queries.GetWorkspaceActivityBySlug;

public record GetWorkspaceActivityBySlugQuery(Guid WorkspaceId, string Slug, int Page = 1, int PageSize = 20) : IQuery<Result<object>>, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceType.Workspace, WorkspaceId, WorkspaceId);
}

public class GetWorkspaceActivityBySlugQueryHandler : IRequestHandler<GetWorkspaceActivityBySlugQuery, Result<object>>
{
    private readonly IApplicationDbContext _context;

    public GetWorkspaceActivityBySlugQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<object>> Handle(GetWorkspaceActivityBySlugQuery request, CancellationToken ct)
    {
        var workspace = await _context.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Slug == request.Slug, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), request.Slug);

        var total = await _context.ActivityLogs
            .CountAsync(a => a.WorkspaceId == workspace.Id, ct);

        var logs = await _context.ActivityLogs
            .AsNoTracking()
            .Where(a => a.WorkspaceId == workspace.Id)
            .OrderByDescending(a => a.Timestamp)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new
            {
                a.Id,
                a.ActorId,
                Action = a.Type.ToString(),
                ResourceType = a.Target.ResourceType.ToString(),
                ResourceId = a.Target.ResourceId,
                a.Timestamp
            })
            .ToListAsync(ct);

        return Result<object>.Success(new { data = logs, total, page = request.Page, pageSize = request.PageSize });
    }
}
