using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;
namespace Notrelix.Application.Features.Workspaces.WorkspaceHome.Queries.GetWorkspaceActivityBySlug;

public record GetWorkspaceActivityBySlugQuery(Guid WorkspaceId, string Slug, int Page = 1, int PageSize = 20) : IQuery<Result<object>>, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceType.Workspace, WorkspaceId, WorkspaceId);
}

public class GetWorkspaceActivityBySlugQueryHandler : IRequestHandler<GetWorkspaceActivityBySlugQuery, Result<object>>
{
    private readonly IWorkspaceDbContext _context;

    public GetWorkspaceActivityBySlugQueryHandler(IWorkspaceDbContext context)
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

        return Result<object>.Success(new { data = Array.Empty<object>(), total = 0, page = request.Page, pageSize = request.PageSize });
    }
}
