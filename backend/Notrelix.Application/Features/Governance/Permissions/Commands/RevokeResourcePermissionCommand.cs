using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Security;
using Notrelix.Application.Common.Models;
using Notrelix.Domain.Common;
using Notrelix.Domain.Governance;
using System.Text.Json;

using ResourceTypeEnum = Notrelix.Domain.Governance.Permissions.ResourceType;

namespace Notrelix.Application.Features.Governance.Commands;

public record RevokeResourcePermissionCommand(
    Guid WorkspaceId,
    string ResourceType,
    Guid ResourceId,
    Guid PermissionId) : IRequest<Result>, IAuthorizeableRequest
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

public class RevokeResourcePermissionCommandHandler : IRequestHandler<RevokeResourcePermissionCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public RevokeResourcePermissionCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(
        RevokeResourcePermissionCommand request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ResourceType>(request.ResourceType, true, out var resourceType))
        {
            return Result.Failure("Invalid resource type format.");
        }

        var permission = await _context.ResourcePermissions
            .FirstOrDefaultAsync(p => p.Id == request.PermissionId &&
                                      p.WorkspaceId == request.WorkspaceId &&
                                      p.ResourceType == resourceType &&
                                      p.ResourceId == request.ResourceId, cancellationToken);

        if (permission == null)
        {
            throw new NotFoundException(nameof(ResourcePermission), request.PermissionId);
        }

        var actorId = _currentUser.UserId;

        // We can completely delete it to prevent unique index conflicts on re-grants
        _context.ResourcePermissions.Remove(permission);

        // Write Audit Log
        var beforeJson = JsonSerializer.Serialize(new
        {
            id = permission.Id,
            level = permission.Level.ToString(),
            expiresAt = permission.ExpiresAt,
            subjectType = permission.SubjectType.ToString(),
            subjectId = permission.SubjectId
        });

        var changes = JsonSerializer.Serialize(new { before = beforeJson, after = (string?)null });

        var auditLog = AuditLog.Record(
            request.WorkspaceId,
            resourceType,
            request.ResourceId,
            "RevokeResourcePermission",
            actorId,
            changes
        );
        _context.AuditLogs.Add(auditLog);

        // Keep ActivityLog for user feed
        var metadata = JsonSerializer.Serialize(new
        {
            subjectType = permission.SubjectType.ToString(),
            subjectId = permission.SubjectId,
            level = permission.Level.ToString()
        });

        var activityLog = ActivityLog.Create(
            request.WorkspaceId,
            actorId,
            "RevokeResourcePermission",
            resourceType,
            request.ResourceId,
            resourceTitle: null,
            metadata: metadata
        );
        _context.ActivityLogs.Add(activityLog);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
