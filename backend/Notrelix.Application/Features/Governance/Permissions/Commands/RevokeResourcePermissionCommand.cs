using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Security;
using Notrelix.Application.Common.Models;
using Notrelix.Domain.Collaboration.Activity;
using Notrelix.Domain.Common;
using Notrelix.Domain.Governance;
using SharedKernel = Notrelix.Domain.SharedKernel;
using System.Text.Json;

namespace Notrelix.Application.Features.Governance.Commands;

public record RevokeResourcePermissionCommand(
    Guid WorkspaceId,
    string ResourceType,
    Guid ResourceId,
    Guid PermissionId) : IRequest<Result>, IAuthorizeableRequest
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

    public class RevokeResourcePermissionCommandHandler : IRequestHandler<RevokeResourcePermissionCommand, Result>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUser _currentUser;
        private readonly IDateTimeProvider _dateTimeProvider;

        public RevokeResourcePermissionCommandHandler(
            IApplicationDbContext context,
            ICurrentUser currentUser,
            IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _currentUser = currentUser;
            _dateTimeProvider = dateTimeProvider;
        }

    public async Task<Result> Handle(
        RevokeResourcePermissionCommand request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<SharedKernel.ResourceType>(request.ResourceType, true, out var resourceType))
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
        var auditLog = AuditLog.Record(
            request.WorkspaceId,
            actorId,
            "RevokeResourcePermission",
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
            subjectType = permission.SubjectType.ToString(),
            subjectId = permission.SubjectId,
            level = permission.Level.ToString()
        });

        var activityLog = ActivityLog.Record(
            request.WorkspaceId,
            actorId,
            ActivityType.Deleted,
            SharedKernel.ResourceRef.Create(resourceType, request.ResourceId),
            _dateTimeProvider.UtcNow,
            ActivityMetadata.Create(SharedKernel.JsonValue.Create(metadata))
        );
        _context.ActivityLogs.Add(activityLog);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
