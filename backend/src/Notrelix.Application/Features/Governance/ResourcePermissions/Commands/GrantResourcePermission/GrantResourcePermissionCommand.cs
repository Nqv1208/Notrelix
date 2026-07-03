using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Governance.DTOs;
using SharedKernel = Notrelix.Domain.SharedKernel;

namespace Notrelix.Application.Features.Governance.ResourcePermissions.Commands.GrantResourcePermission;

public record GrantResourcePermissionCommand(
    Guid WorkspaceId,
    SharedKernel.ResourceType ResourceType,
    Guid ResourceId,
    string SubjectType,
    Guid SubjectId,
    string Level,
    DateTime? ExpiresAt = null) : ICommand<Result<ResourcePermissionDto>>, IRequirePermission, ITransactionalRequest
{
    PermissionAction IRequirePermission.Action => ResourceType switch
    {
        SharedKernel.ResourceType.Board => PermissionAction.ManageBoardPermission,
        SharedKernel.ResourceType.Page => PermissionAction.SharePage,
        _ => PermissionAction.ManageWorkspace
    };
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceType, ResourceId, WorkspaceId);
}

public class GrantResourcePermissionCommandHandler : IRequestHandler<GrantResourcePermissionCommand, Result<ResourcePermissionDto>>
{
    private readonly IGovernanceDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAuditService _auditService;
    private readonly ICurrentTenantContext _tenant;

    public GrantResourcePermissionCommandHandler(
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

    public async Task<Result<ResourcePermissionDto>> Handle(
        GrantResourcePermissionCommand request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<PermissionSubjectType>(request.SubjectType, true, out var subjectType) ||
            !Enum.TryParse<PermissionLevel>(request.Level, true, out var level))
        {
            return Result<ResourcePermissionDto>.Failure("Invalid format for enum parameters.");
        }

        var existingPermission = await _context.ResourcePermissions
            .FirstOrDefaultAsync(p => p.WorkspaceId == request.WorkspaceId &&
                                      p.ResourceType == request.ResourceType &&
                                      p.ResourceId == request.ResourceId &&
                                      p.SubjectType == subjectType &&
                                      p.SubjectId == request.SubjectId, cancellationToken);

        var actorId = _currentUser.UserId;

        var granterPermission = await _context.ResourcePermissions
            .Where(p => p.WorkspaceId == request.WorkspaceId &&
                        p.ResourceType == request.ResourceType &&
                        p.ResourceId == request.ResourceId &&
                        p.SubjectType == PermissionSubjectType.User &&
                        p.SubjectId == actorId &&
                        !p.IsDeleted)
            .OrderByDescending(p => p.Level)
            .FirstOrDefaultAsync(cancellationToken);

        var granterLevel = granterPermission?.Level ?? PermissionLevel.None;

        if (existingPermission != null)
        {
            _context.ResourcePermissions.Remove(existingPermission);
        }

        var permission = ResourcePermission.Grant(
            _tenant.RequireAccountId(),
            request.WorkspaceId,
            request.ResourceType,
            request.ResourceId,
            subjectType,
            request.SubjectId,
            level,
            granterLevel,
            actorId,
            _dateTimeProvider.UtcNow);

        _context.ResourcePermissions.Add(permission);

        await _auditService.RecordAsync(
            request.WorkspaceId,
            actorId,
            "GrantResourcePermission",
            SharedKernel.ResourceRef.Create(request.ResourceType, request.ResourceId),
            AuditMetadata.Create(),
            AuditSeverity.Info,
            cancellationToken: cancellationToken);

        var dto = new ResourcePermissionDto(
            permission.Id,
            permission.WorkspaceId,
            permission.ResourceType.ToString(),
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
