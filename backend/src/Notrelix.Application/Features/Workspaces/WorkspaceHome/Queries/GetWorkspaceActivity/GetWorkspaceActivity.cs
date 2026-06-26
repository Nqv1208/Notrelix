using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.Workspaces.WorkspaceHome.Queries.GetWorkspaceActivity;

public record GetWorkspaceActivityQuery(Guid WorkspaceId, int Page = 1, int PageSize = 20) : IQuery<Result<object>>, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceType.Workspace, WorkspaceId, WorkspaceId);
}

public class GetWorkspaceActivityQueryHandler : IRequestHandler<GetWorkspaceActivityQuery, Result<object>>
{
    private readonly IApplicationDbContext _context;

    public GetWorkspaceActivityQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<object>> Handle(GetWorkspaceActivityQuery request, CancellationToken ct)
    {
        var workspaceExists = await _context.Workspaces
            .AsNoTracking()
            .AnyAsync(w => w.Id == request.WorkspaceId && w.Status == WorkspaceStatus.Active && !w.IsDeleted, ct);

        if (!workspaceExists)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var total = await _context.ActivityLogs
            .CountAsync(a => a.WorkspaceId == request.WorkspaceId, ct);

        var logs = await _context.ActivityLogs
            .AsNoTracking()
            .Where(a => a.WorkspaceId == request.WorkspaceId)
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
