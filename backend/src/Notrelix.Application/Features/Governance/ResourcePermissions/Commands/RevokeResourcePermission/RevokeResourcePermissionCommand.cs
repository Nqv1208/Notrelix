using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Models;
using Notrelix.Domain.Collaboration.Activity;
using Notrelix.Domain.Common;
using Notrelix.Domain.Governance;
using SharedKernel = Notrelix.Domain.SharedKernel;
using System.Text.Json;

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
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUser _currentUser;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IAuditService _auditService;

        public RevokeResourcePermissionCommandHandler(
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
            SharedKernel.ResourceRef.Create(request.ResourceType, request.ResourceId),
            _dateTimeProvider.UtcNow,
            ActivityMetadata.Create(SharedKernel.JsonValue.Create(metadata))
        );
        _context.ActivityLogs.Add(activityLog);

        return Result.Success();
    }
}
