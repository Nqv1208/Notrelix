using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.DTOs;
using SharedKernel = Notrelix.Domain.SharedKernel;
using System.Text.Json;

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
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAuditService _auditService;

    public GrantResourcePermissionCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        IAuditService auditService)
    {
        _context = context;
        _currentUser = currentUser;
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

        var existingPermission = await _context.ResourcePermissions
            .FirstOrDefaultAsync(p => p.WorkspaceId == request.WorkspaceId &&
                                      p.ResourceType == request.ResourceType &&
                                      p.ResourceId == request.ResourceId &&
                                      p.SubjectType == subjectType &&
                                      p.SubjectId == request.SubjectId, cancellationToken);

        var actorId = _currentUser.UserId;

        if (existingPermission != null)
        {
            _context.ResourcePermissions.Remove(existingPermission);
        }

        var permission = ResourcePermission.Grant(
            request.WorkspaceId,
            request.ResourceType,
            request.ResourceId,
            subjectType,
            request.SubjectId,
            level,
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

        // Keep ActivityLog for user feed
        var metadata = JsonSerializer.Serialize(new
        {
            subjectType = request.SubjectType,
            subjectId = request.SubjectId,
            level = request.Level,
            expiresAt = request.ExpiresAt
        });

        var activityLog = ActivityLog.Record(
            request.WorkspaceId,
            actorId,
            ActivityType.Created,
            SharedKernel.ResourceRef.Create(request.ResourceType, request.ResourceId),
            _dateTimeProvider.UtcNow,
            ActivityMetadata.Create(SharedKernel.JsonValue.Create(metadata))
        );
        _context.ActivityLogs.Add(activityLog);

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
