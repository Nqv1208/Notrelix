using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Security;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.DTOs;
using Notrelix.Domain.Collaboration.Activity;
using Notrelix.Domain.Common;
using Notrelix.Domain.Governance;
using Notrelix.Domain.Governance.Permissions;
using SharedKernel = Notrelix.Domain.SharedKernel;
using System.Text.Json;

namespace Notrelix.Application.Features.Governance.Commands;

public record GrantResourcePermissionCommand(
    Guid WorkspaceId,
    string ResourceType,
    Guid ResourceId,
    string SubjectType,
    Guid SubjectId,
    string Level,
    DateTime? ExpiresAt = null) : IRequest<Result<ResourcePermissionDto>>, IAuthorizeableRequest
{
    SharedKernel.ResourceType IAuthorizeableRequest.ResourceType => Enum.Parse<SharedKernel.ResourceType>(ResourceType, true);
    Guid IAuthorizeableRequest.ResourceId => ResourceId;
    PermissionAction IAuthorizeableRequest.Action => Enum.Parse<SharedKernel.ResourceType>(ResourceType, true) switch
    {
        SharedKernel.ResourceType.Board => PermissionAction.ManageBoardPermission,
        SharedKernel.ResourceType.Page => PermissionAction.SharePage,
        _ => PermissionAction.ManageWorkspace
    };
}

    public class GrantResourcePermissionCommandHandler : IRequestHandler<GrantResourcePermissionCommand, Result<ResourcePermissionDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUser _currentUser;
        private readonly IDateTimeProvider _dateTimeProvider;

        public GrantResourcePermissionCommandHandler(
            IApplicationDbContext context,
            ICurrentUser currentUser,
            IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _currentUser = currentUser;
            _dateTimeProvider = dateTimeProvider;
        }

    public async Task<Result<ResourcePermissionDto>> Handle(
        GrantResourcePermissionCommand request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<SharedKernel.ResourceType>(request.ResourceType, true, out var resourceType) ||
            !Enum.TryParse<PermissionSubjectType>(request.SubjectType, true, out var subjectType) ||
            !Enum.TryParse<PermissionLevel>(request.Level, true, out var level))
        {
            return Result<ResourcePermissionDto>.Failure("Invalid format for enum parameters.");
        }

        var existingPermission = await _context.ResourcePermissions
            .FirstOrDefaultAsync(p => p.WorkspaceId == request.WorkspaceId &&
                                      p.ResourceType == resourceType &&
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
            resourceType,
            request.ResourceId,
            subjectType,
            request.SubjectId,
            level,
            actorId,
            _dateTimeProvider.UtcNow);

        _context.ResourcePermissions.Add(permission);

        // Write Audit Log
        var auditLog = AuditLog.Record(
            request.WorkspaceId,
            actorId,
            "GrantResourcePermission",
            SharedKernel.ResourceRef.Create(resourceType, request.ResourceId),
            AuditMetadata.Create(),
            AuditSeverity.Info,
            "",
            "",
            DateTimeOffset.UtcNow
        );
        _context.AuditLogs.Add(auditLog);

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
            SharedKernel.ResourceRef.Create(resourceType, request.ResourceId),
            _dateTimeProvider.UtcNow,
            ActivityMetadata.Create(SharedKernel.JsonValue.Create(metadata))
        );
        _context.ActivityLogs.Add(activityLog);

        await _context.SaveChangesAsync(cancellationToken);

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
