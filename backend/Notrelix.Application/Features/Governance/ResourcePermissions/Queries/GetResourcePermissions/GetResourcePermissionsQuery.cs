using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.DTOs;
using Notrelix.Domain.Common;
using SharedKernel = Notrelix.Domain.SharedKernel;

namespace Notrelix.Application.Features.Governance.ResourcePermissions.Queries.GetResourcePermissions;

public record GetResourcePermissionsQuery(
    Guid WorkspaceId,
    string ResourceType,
    Guid ResourceId) : IQuery<Result<List<ResourcePermissionDto>>>, IRequirePermission
{
    PermissionAction IRequirePermission.Action => Enum.Parse<SharedKernel.ResourceType>(ResourceType, true) switch
    {
        SharedKernel.ResourceType.Board => PermissionAction.ManageBoardPermission,
        SharedKernel.ResourceType.Page => PermissionAction.SharePage,
        _ => PermissionAction.ManageWorkspace
    };
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(Enum.Parse<SharedKernel.ResourceType>(ResourceType, true), ResourceId, WorkspaceId);
}

public class GetResourcePermissionsQueryHandler : IRequestHandler<GetResourcePermissionsQuery, Result<List<ResourcePermissionDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetResourcePermissionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<ResourcePermissionDto>>> Handle(
        GetResourcePermissionsQuery request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<SharedKernel.ResourceType>(request.ResourceType, true, out var resourceType))
        {
            return Result<List<ResourcePermissionDto>>.Failure("Invalid resource type format.");
        }

        var permissions = await _context.ResourcePermissions
            .AsNoTracking()
            .Where(p => p.WorkspaceId == request.WorkspaceId &&
                        p.ResourceType == resourceType &&
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
