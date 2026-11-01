using Notrelix.Application.Common.Realtime;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Governance.Public.Access;

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
    private readonly IResourceAccessChangeRecorder _changes;
    private readonly IBoardSharingSummary _sharing;
    private readonly IAccessChangeSignal _access;

    public RevokeResourcePermissionCommandHandler(
        IGovernanceDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        IAuditService auditService,
        IResourceAccessChangeRecorder changes,
        IBoardSharingSummary sharing,
        IAccessChangeSignal access)
    {
        _sharing = sharing;
        _access = access;
        _changes = changes;
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
            // Revoked already (a retry, or two managers removing the same entry): nothing left to do.
            // An id that never belonged to this resource in this workspace is still NotFound.
            var alreadyRevoked = await _context.ResourcePermissions
                .IgnoreQueryFilters()
                .AnyAsync(p => p.Id == request.PermissionId &&
                               p.WorkspaceId == workspaceId &&
                               p.ResourceKind == kind &&
                               p.ResourceId == request.ResourceId &&
                               p.DeletedAt != null, cancellationToken);
            if (alreadyRevoked)
            {
                return Result.Success();
            }

            throw new NotFoundException(nameof(ResourcePermission), request.PermissionId);
        }

        var actorId = _requestContext.UserId;

        if (kind.Value == "work-management.board")
        {
            await BoardManagerFloor.EnsureAnotherManagerAsync(
                _sharing, request.ResourceId, permission.SubjectType, permission.SubjectId, cancellationToken);
        }

        permission.Revoke(actorId, _dateTimeProvider.UtcNow);

        if (kind.Value == "work-management.board")
        {
            await _changes.RecordAsync(
                new ResourceAccessChangeRecord(kind.Value, request.ResourceId, AccessChangeKinds.Removed, permission.SubjectType.ToString(), permission.SubjectId, AccessLevels.OfRank((int)permission.Level), null),
                cancellationToken);
            // The person (or the Team's people) may lose the board now, not at the next periodic pass.
            if (permission.SubjectType == PermissionSubjectType.User)
                _access.BoardPerson(request.ResourceId, permission.SubjectId);
            else
                _access.Board(request.ResourceId);
        }

        await _auditService.RecordAsync(
            workspaceId,
            actorId,
            "RevokeResourcePermission",
            ResourceRef.Create(kind, request.ResourceId),
            AuditMetadata.Create(),
            AuditSeverity.Info,
            cancellationToken: cancellationToken,
            change: new AuditChange(
                permission.SubjectType.ToString(),
                permission.SubjectId,
                new { level = AccessLevels.OfRank((int)permission.Level) },
                null));

        return Result.Success();
    }
}
