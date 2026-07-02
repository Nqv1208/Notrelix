using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Governance.DTOs;
using SharedKernel = Notrelix.Domain.SharedKernel;

namespace Notrelix.Application.Features.Governance.ResourcePermissions.Queries.GetResourcePermissions;

public record GetResourcePermissionsQuery(
    Guid WorkspaceId,
    SharedKernel.ResourceType ResourceType,
    Guid ResourceId) : IQuery<Result<List<ResourcePermissionDto>>>, IRequirePermission
{
    PermissionAction IRequirePermission.Action => ResourceType switch
    {
        SharedKernel.ResourceType.Board => PermissionAction.ManageBoardPermission,
        SharedKernel.ResourceType.Page => PermissionAction.SharePage,
        _ => PermissionAction.ManageWorkspace
    };
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceType, ResourceId, WorkspaceId);
}

public class GetResourcePermissionsQueryHandler : IRequestHandler<GetResourcePermissionsQuery, Result<List<ResourcePermissionDto>>>
{
    private readonly IGovernanceDbContext _context;

    public GetResourcePermissionsQueryHandler(IGovernanceDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<ResourcePermissionDto>>> Handle(
        GetResourcePermissionsQuery request,
        CancellationToken cancellationToken)
    {
        var permissions = await _context.ResourcePermissions
            .AsNoTracking()
            .Where(p => p.WorkspaceId == request.WorkspaceId &&
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
