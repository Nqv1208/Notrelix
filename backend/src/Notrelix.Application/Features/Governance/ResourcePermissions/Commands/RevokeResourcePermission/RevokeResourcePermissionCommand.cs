using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Abstractions;

namespace Notrelix.Application.Features.Governance.ResourcePermissions.Commands.RevokeResourcePermission;

public record RevokeResourcePermissionCommand(
    string ResourceKind,
    Guid ResourceId,
    Guid PermissionId) : ICommand<Result>, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IRequireRevokePermission, IRequirePermissionTarget, IWriteRequest
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

    Guid IRequireRevokePermission.TargetPermissionId => PermissionId;

    string? IRequirePermissionTarget.TargetSubjectType => null;
    Guid? IRequirePermissionTarget.TargetSubjectId => null;
    Guid? IRequirePermissionTarget.TargetPermissionId => PermissionId;

    private static ResourceKind ParseKind(string value) =>
        global::Notrelix.Domain.SharedKernel.ResourceKind.TryCreate(value, out var kind)
            ? kind
            : throw new ArgumentException($"Invalid resource kind '{value}'. Expected a canonical kind such as 'work-management.board'.", nameof(value));
}

public class RevokeResourcePermissionCommandHandler : IRequestHandler<RevokeResourcePermissionCommand, Result>
{
    private readonly IGovernanceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAuditService _auditService;

    public RevokeResourcePermissionCommandHandler(
        IGovernanceDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        IAuditService auditService)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _auditService = auditService;
    }

    public async Task<Result> Handle(
        RevokeResourcePermissionCommand request,
        CancellationToken cancellationToken)
    {
        var workspaceId = _requestContext.RequireWorkspaceId();
        var kind = request.Kind;

        var permission = await _context.ResourcePermissions
            .FirstOrDefaultAsync(p => p.Id == request.PermissionId &&
                                      p.WorkspaceId == workspaceId &&
                                      p.ResourceKind == kind &&
                                      p.ResourceId == request.ResourceId, cancellationToken);

        if (permission == null)
        {
            throw new NotFoundException(nameof(ResourcePermission), request.PermissionId);
        }

        var actorId = _requestContext.UserId;

        permission.Revoke(actorId, _dateTimeProvider.UtcNow);

        await _auditService.RecordAsync(
            workspaceId,
            actorId,
            "RevokeResourcePermission",
            ResourceRef.Create(kind, request.ResourceId),
            AuditMetadata.Create(),
            AuditSeverity.Info,
            cancellationToken: cancellationToken);

        return Result.Success();
    }
}
