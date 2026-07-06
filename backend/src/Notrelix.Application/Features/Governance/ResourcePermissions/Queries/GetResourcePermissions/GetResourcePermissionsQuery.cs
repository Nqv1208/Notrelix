using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Governance.DTOs;

namespace Notrelix.Application.Features.Governance.ResourcePermissions.Queries.GetResourcePermissions;

public record GetResourcePermissionsQuery(
    ResourceType ResourceType,
    Guid ResourceId) : IQuery<Result<List<ResourcePermissionDto>>>, IResourceScopedRequest, IRequirePermission
{
    PermissionAction IRequirePermission.Action => ResourceType switch
    {
        ResourceType.Board => PermissionAction.ManageBoardPermission,
        ResourceType.Page => PermissionAction.SharePage,
        _ => PermissionAction.ManageWorkspace
    };
    ResourceRef IResourceScopedRequest.Resource => ResourceRef.Create(ResourceType, ResourceId);
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceType, ResourceId);
}

public class GetResourcePermissionsQueryHandler : IRequestHandler<GetResourcePermissionsQuery, Result<List<ResourcePermissionDto>>>
{
    private readonly IGovernanceDbContext _context;
    private readonly ICurrentTenantContext _tenant;

    public GetResourcePermissionsQueryHandler(IGovernanceDbContext context, ICurrentTenantContext tenant)
    {
        _context = context;
        _tenant = tenant;
    }

    public async Task<Result<List<ResourcePermissionDto>>> Handle(
        GetResourcePermissionsQuery request,
        CancellationToken cancellationToken)
    {
        var workspaceId = _tenant.RequireWorkspaceId();
        var permissions = await _context.ResourcePermissions
            .AsNoTracking()
            .Where(p => p.WorkspaceId == workspaceId &&
                        p.ResourceType == request.ResourceType &&
                        p.ResourceId == request.ResourceId)
            .Select(p => new ResourcePermissionDto(
                p.Id,
                p.WorkspaceId,
                p.ResourceType.ToString(),
                p.ResourceId,
                p.SubjectType.ToString(),
                p.SubjectId,
                p.Level.ToString(),
                p.CreatedBy,
                p.IsDeleted,
                p.DeletedAt))
            .ToListAsync(cancellationToken);

        return Result<List<ResourcePermissionDto>>.Success(permissions);
    }
}
