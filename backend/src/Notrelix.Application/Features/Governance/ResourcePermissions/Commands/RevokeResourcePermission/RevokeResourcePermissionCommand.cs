using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Abstractions;
using SharedKernel = Notrelix.Domain.SharedKernel;

namespace Notrelix.Application.Features.Governance.ResourcePermissions.Commands.RevokeResourcePermission;

public record RevokeResourcePermissionCommand(
    Guid WorkspaceId,
    SharedKernel.ResourceType ResourceType,
    Guid ResourceId,
    Guid PermissionId) : ICommand<Result>, IRequirePermission, ITransactionalRequest
{
    PermissionAction IRequirePermission.Action => ResourceType switch
    {
        SharedKernel.ResourceType.Board => PermissionAction.ManageBoardPermission,
        SharedKernel.ResourceType.Page => PermissionAction.SharePage,
        _ => PermissionAction.ManageWorkspace
    };
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceType, ResourceId, WorkspaceId);
}

public class RevokeResourcePermissionCommandHandler : IRequestHandler<RevokeResourcePermissionCommand, Result>
{
    private readonly IGovernanceDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAuditService _auditService;

    public RevokeResourcePermissionCommandHandler(
        IGovernanceDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        IAuditService auditService)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _auditService = auditService;
    }

    public async Task<Result> Handle(
        RevokeResourcePermissionCommand request,
        CancellationToken cancellationToken)
    {
        var permission = await _context.ResourcePermissions
            .FirstOrDefaultAsync(p => p.Id == request.PermissionId &&
                                      p.WorkspaceId == request.WorkspaceId &&
                                      p.ResourceType == request.ResourceType &&
                                      p.ResourceId == request.ResourceId, cancellationToken);

        if (permission == null)
        {
            throw new NotFoundException(nameof(ResourcePermission), request.PermissionId);
        }

        var actorId = _currentUser.UserId;

        // We can completely delete it to prevent unique index conflicts on re-grants
        _context.ResourcePermissions.Remove(permission);

        await _auditService.RecordAsync(
            request.WorkspaceId,
            actorId,
            "RevokeResourcePermission",
            SharedKernel.ResourceRef.Create(request.ResourceType, request.ResourceId),
            AuditMetadata.Create(),
            AuditSeverity.Info,
            cancellationToken: cancellationToken);

        return Result.Success();
    }
}
