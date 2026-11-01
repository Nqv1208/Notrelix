using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Governance.DTOs;

namespace Notrelix.Application.Features.Governance.ResourcePermissions.Queries.GetResourcePermissions;

public record GetResourcePermissionsQuery(
    string ResourceKind,
    Guid ResourceId) : IQuery<Result<List<ResourcePermissionDto>>>, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest, IRequirePermission
{
    internal ResourceKind Kind => ParseKind(ResourceKind);

    PermissionAction IRequirePermission.Action => Kind.Value switch
    {
        "work-management.board" => PermissionAction.ManageBoardPermission,
        "documents.page" => PermissionAction.ManagePagePermission,
        _ => PermissionAction.ManageWorkspace
    };
    ResourceRef IResourceScopedRequest.Resource => ResourceRef.Create(Kind, ResourceId);
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(Kind, ResourceId);

    private static ResourceKind ParseKind(string value) =>
        global::Notrelix.Domain.SharedKernel.ResourceKind.TryCreate(value, out var kind)
            ? kind
            : throw new ArgumentException($"Invalid resource kind '{value}'. Expected a canonical kind such as 'work-management.board'.", nameof(value));
}

public class GetResourcePermissionsQueryHandler : IRequestHandler<GetResourcePermissionsQuery, Result<List<ResourcePermissionDto>>>
{
    private const int MaxEntries = 500;

    private readonly IGovernanceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;

    public GetResourcePermissionsQueryHandler(IGovernanceDbContext context, ICurrentRequestContext requestContext)
    {
        _context = context;
        _requestContext = requestContext;
    }

    public async Task<Result<List<ResourcePermissionDto>>> Handle(
        GetResourcePermissionsQuery request,
        CancellationToken cancellationToken)
    {
        var workspaceId = _requestContext.RequireWorkspaceId();
        var kind = request.Kind;
        // Revoked rows are not entries; a resource's explicit entries are people and Teams, so a cap is a guard, not a page.
        var rows = await _context.ResourcePermissions
            .AsNoTracking()
            .Where(p => p.WorkspaceId == workspaceId &&
                        p.ResourceKind == kind &&
                        p.ResourceId == request.ResourceId &&
                        p.DeletedAt == null)
            .OrderBy(p => p.CreatedAt)
            .ThenBy(p => p.Id)
            .Take(MaxEntries)
            .Select(p => new
            {
                p.Id,
                p.WorkspaceId,
                Kind = p.ResourceKind,
                p.ResourceId,
                p.SubjectType,
                p.SubjectId,
                p.Level,
                p.CreatedBy,
                p.DeletedAt,
                p.Version
            })
            .ToListAsync(cancellationToken);

        var permissions = rows
            .Select(p => new ResourcePermissionDto(
                p.Id,
                p.WorkspaceId,
                p.Kind.Value,
                p.ResourceId,
                p.SubjectType.ToString(),
                p.SubjectId,
                p.Level.ToString(),
                p.CreatedBy,
                p.DeletedAt != null,
                p.DeletedAt,
                p.Version))
            .ToList();

        return Result<List<ResourcePermissionDto>>.Success(permissions);
    }
}
