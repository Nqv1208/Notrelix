using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.WorkspaceHome.Queries.GetWorkspaceActivity;

public record GetWorkspaceActivityQuery(Guid WorkspaceId, int Page = 1, int PageSize = 20) : IQuery<Result<object>>, IWorkspaceRequest, IRequirePermission
{
    Guid IWorkspaceRequest.WorkspaceId => WorkspaceId;
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceType.Workspace, WorkspaceId, WorkspaceId);
}

public class GetWorkspaceActivityQueryHandler : IRequestHandler<GetWorkspaceActivityQuery, Result<object>>
{
    private readonly IWorkspaceDbContext _context;

    public GetWorkspaceActivityQueryHandler(IWorkspaceDbContext context)
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

        return Result<object>.Success(new { data = Array.Empty<object>(), total = 0, page = request.Page, pageSize = request.PageSize });
    }
}
