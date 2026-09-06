using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Governance.DTOs;

namespace Notrelix.Application.Features.Governance.ResourcePermissions.Commands.GrantResourcePermission;

public record GrantResourcePermissionCommand(
    string ResourceKind,
    Guid ResourceId,
    string SubjectType,
    Guid SubjectId,
    string Level,
    DateTime? ExpiresAt = null) : ICommand<Result<ResourcePermissionDto>>, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IRequireGrantPermission, IRequirePermissionTarget, IWriteRequest
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

    // Governance owns the level vocabulary and its rank mapping; the pipeline
    // seam compares technical integer ranks only. The mapping mirrors the
    // PermissionLevel enum ordering: None=0, Viewer=1, Commenter=2, Editor=3,
    // Manager=4, Owner=5.
    int IRequireGrantPermission.RequestedPermissionRank =>
        Enum.TryParse<PermissionLevel>(Level, true, out var level) ? (int)level : 0;

    string? IRequirePermissionTarget.TargetSubjectType => SubjectType;
    Guid? IRequirePermissionTarget.TargetSubjectId => SubjectId;
    Guid? IRequirePermissionTarget.TargetPermissionId => null;

    private static ResourceKind ParseKind(string value) =>
        global::Notrelix.Domain.SharedKernel.ResourceKind.TryCreate(value, out var kind)
            ? kind
            : throw new ArgumentException($"Invalid resource kind '{value}'. Expected a canonical kind such as 'work-management.board'.", nameof(value));
}

public class GrantResourcePermissionCommandHandler : IRequestHandler<GrantResourcePermissionCommand, Result<ResourcePermissionDto>>
{
    private readonly IGovernanceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAuditService _auditService;

    public GrantResourcePermissionCommandHandler(
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

    public async Task<Result<ResourcePermissionDto>> Handle(
        GrantResourcePermissionCommand request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<PermissionSubjectType>(request.SubjectType, true, out var subjectType) ||
            !Enum.TryParse<PermissionLevel>(request.Level, true, out var level))
        {
            return Result<ResourcePermissionDto>.Failure("Invalid format for enum parameters.");
        }

        var workspaceId = _requestContext.RequireWorkspaceId();
        var accountId = _requestContext.RequireAccountId();
        var actorId = _requestContext.UserId;
        var kind = request.Kind;
        var now = _dateTimeProvider.UtcNow;

        var existingPermission = await _context.ResourcePermissions
            .FirstOrDefaultAsync(p => p.WorkspaceId == workspaceId &&
                                      p.ResourceKind == kind &&
                                      p.ResourceId == request.ResourceId &&
                                      p.SubjectType == subjectType &&
                                      p.SubjectId == request.SubjectId, cancellationToken);

        ResourcePermission permission;
        if (existingPermission != null)
        {
            if (existingPermission.Level == level)
            {
                permission = existingPermission;
            }
            else
            {
                existingPermission.ChangeLevel(level, actorId, now);
                permission = existingPermission;
            }
        }
        else
        {
            permission = ResourcePermission.Grant(
                accountId,
                workspaceId,
                kind,
                request.ResourceId,
                subjectType,
                request.SubjectId,
                level,
                actorId,
                now);

            _context.ResourcePermissions.Add(permission);
        }

        await _auditService.RecordAsync(
            workspaceId,
            actorId,
            "GrantResourcePermission",
            ResourceRef.Create(kind, request.ResourceId),
            AuditMetadata.Create(),
            AuditSeverity.Info,
            cancellationToken: cancellationToken);

        var dto = new ResourcePermissionDto(
            permission.Id,
            permission.WorkspaceId,
            permission.ResourceKind.Value,
            permission.ResourceId,
            permission.SubjectType.ToString(),
            permission.SubjectId,
            permission.Level.ToString(),
            permission.CreatedBy,
            permission.IsDeleted,
            permission.DeletedAt);

        return Result<ResourcePermissionDto>.Success(dto);
    }
}
