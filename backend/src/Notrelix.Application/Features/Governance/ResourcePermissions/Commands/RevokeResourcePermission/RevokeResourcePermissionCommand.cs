using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Abstractions;

namespace Notrelix.Application.Features.Governance.ResourcePermissions.Commands.RevokeResourcePermission;

public record RevokeResourcePermissionCommand(
    ResourceType ResourceType,
    Guid ResourceId,
    Guid PermissionId) : ICommand<Result>, IResourceScopedRequest, IRequirePermission, ITransactionalRequest
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

public class RevokeResourcePermissionCommandHandler : IRequestHandler<RevokeResourcePermissionCommand, Result>
{
    private readonly IGovernanceDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAuditService _auditService;
    private readonly ICurrentTenantContext _tenant;

    public RevokeResourcePermissionCommandHandler(
        IGovernanceDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        IAuditService auditService,
        ICurrentTenantContext tenant)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _auditService = auditService;
        _tenant = tenant;
    }

    public async Task<Result> Handle(
        RevokeResourcePermissionCommand request,
        CancellationToken cancellationToken)
    {
        var workspaceId = _tenant.RequireWorkspaceId();

        var permission = await _context.ResourcePermissions
            .FirstOrDefaultAsync(p => p.Id == request.PermissionId &&
                                      p.WorkspaceId == workspaceId &&
                                      p.ResourceType == request.ResourceType &&
                                      p.ResourceId == request.ResourceId, cancellationToken);

        if (permission == null)
        {
            throw new NotFoundException(nameof(ResourcePermission), request.PermissionId);
        }

        var actorId = _currentUser.UserId;

        _context.ResourcePermissions.Remove(permission);

        await _auditService.RecordAsync(
            workspaceId,
            actorId,
            "RevokeResourcePermission",
            ResourceRef.Create(request.ResourceType, request.ResourceId),
            AuditMetadata.Create(),
            AuditSeverity.Info,
            cancellationToken: cancellationToken);

        return Result.Success();
    }
}
