using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Security;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.DTOs;
using Notrelix.Domain.Common;
using Notrelix.Domain.Governance;
using System.Text.Json;

using ResourceTypeEnum = Notrelix.Domain.Governance.Permissions.ResourceType;

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
    ResourceTypeEnum IAuthorizeableRequest.ResourceType => Enum.Parse<ResourceTypeEnum>(ResourceType, true);
    Guid IAuthorizeableRequest.ResourceId => ResourceId;
    PermissionAction IAuthorizeableRequest.Action => Enum.Parse<ResourceTypeEnum>(ResourceType, true) switch
    {
        ResourceTypeEnum.Board => PermissionAction.ManageBoardPermission,
        ResourceTypeEnum.Page => PermissionAction.SharePage,
        _ => PermissionAction.ManageWorkspace
    };
}

public class GrantResourcePermissionCommandHandler : IRequestHandler<GrantResourcePermissionCommand, Result<ResourcePermissionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GrantResourcePermissionCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<ResourcePermissionDto>> Handle(
        GrantResourcePermissionCommand request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ResourceType>(request.ResourceType, true, out var resourceType) ||
            !Enum.TryParse<SubjectType>(request.SubjectType, true, out var subjectType) ||
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

        string? beforeJson = null;
        if (existingPermission != null)
        {
            beforeJson = JsonSerializer.Serialize(new
            {
                id = existingPermission.Id,
                level = existingPermission.Level.ToString(),
                expiresAt = existingPermission.ExpiresAt
            });
            _context.ResourcePermissions.Remove(existingPermission);
        }

        var permission = ResourcePermission.Create(
            request.WorkspaceId,
            resourceType,
            request.ResourceId,
            subjectType,
            request.SubjectId,
            level,
            actorId,
            request.ExpiresAt);

        _context.ResourcePermissions.Add(permission);

        // Write Audit Log
        var afterJson = JsonSerializer.Serialize(new
        {
            id = permission.Id,
            level = permission.Level.ToString(),
            expiresAt = permission.ExpiresAt
        });

        var changes = JsonSerializer.Serialize(new { before = beforeJson, after = afterJson });

        var auditLog = AuditLog.Record(
            request.WorkspaceId,
            resourceType,
            request.ResourceId,
            "GrantResourcePermission",
            actorId,
            changes
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

        var activityLog = ActivityLog.Create(
            request.WorkspaceId,
            actorId,
            "GrantResourcePermission",
            resourceType,
            request.ResourceId,
            resourceTitle: null,
            metadata: metadata
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
            permission.GrantedBy,
            permission.ExpiresAt,
            permission.IsRevoked,
            permission.RevokedAt);

        return Result<ResourcePermissionDto>.Success(dto);
    }
}
