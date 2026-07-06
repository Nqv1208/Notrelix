using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Workspaces.DTOs;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Workspaces.Queries.GetWorkspace;

public record GetWorkspaceQuery(Guid WorkspaceId) : IQuery<Result<WorkspaceDto>>, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceType.Workspace, WorkspaceId, WorkspaceId);
}

public class GetWorkspaceQueryHandler : IRequestHandler<GetWorkspaceQuery, Result<WorkspaceDto>>
{
    private readonly IWorkspaceDbContext _context;

    public GetWorkspaceQueryHandler(IWorkspaceDbContext context)
    {
        _context = context;
    }

    public async Task<Result<WorkspaceDto>> Handle(GetWorkspaceQuery request, CancellationToken ct)
    {
        var workspace = await _context.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId && w.Status == WorkspaceStatus.Active && !w.IsDeleted, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var memberCount = await _context.WorkspaceMembers
            .CountAsync(m => m.WorkspaceId == workspace.Id, ct);

        return Result<WorkspaceDto>.Success(new WorkspaceDto(
            workspace.Id,
            workspace.Name,
            workspace.Slug,
            workspace.Description,
            workspace.IsPersonal,
            "free",
            null,
            null,
            null,
            workspace.Status == WorkspaceStatus.Archived,
            memberCount,
            workspace.CreatedAt.DateTime,
            null
        ));
    }
}
