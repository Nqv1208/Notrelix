using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Models;
using Notrelix.Domain.Collaboration.Activity;
using Notrelix.Domain.Common;
using Notrelix.Domain.Governance;
using Notrelix.Domain.SharedKernel;
using System.Text.Json;

namespace Notrelix.Application.Features.Governance.ShareLinks.Commands.DisableShareLink;

public record DisableShareLinkCommand(
    Guid WorkspaceId,
    string ResourceTypeValue,
    Guid ResourceId,
    Guid ShareLinkId) : ICommand<Result>, IRequirePermission, ITransactionalRequest
{
    private ResourceType ResourceType => Enum.Parse<ResourceType>(ResourceTypeValue, true);

    PermissionAction IRequirePermission.Action => ResourceType switch
    {
        ResourceType.Board => PermissionAction.ShareBoardView,
        ResourceType.Page => PermissionAction.SharePage,
        _ => PermissionAction.ManageWorkspace
    };
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceType, ResourceId, WorkspaceId);
}

    public class DisableShareLinkCommandHandler : IRequestHandler<DisableShareLinkCommand, Result>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUser _currentUser;
        private readonly IDateTimeProvider _dateTimeProvider;

        public DisableShareLinkCommandHandler(
            IApplicationDbContext context,
            ICurrentUser currentUser,
            IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _currentUser = currentUser;
            _dateTimeProvider = dateTimeProvider;
        }

    public async Task<Result> Handle(
        DisableShareLinkCommand request,
        CancellationToken cancellationToken)
    {
        var shareLink = await _context.ShareLinks
            .FirstOrDefaultAsync(s => s.Id == request.ShareLinkId, cancellationToken);

        if (shareLink == null)
        {
            throw new NotFoundException(nameof(ShareLink), request.ShareLinkId);
        }

        var userId = _currentUser.UserId;

        shareLink.Disable(userId, _dateTimeProvider.UtcNow);

        // Write Audit Log
        var metadata = JsonSerializer.Serialize(new
        {
            shareLinkId = shareLink.Id,
            level = shareLink.AccessMode.ToString()
        });

        var auditLog = ActivityLog.Record(
            shareLink.WorkspaceId,
            userId,
            ActivityType.Updated,
            ResourceRef.Create(shareLink.ResourceType, shareLink.ResourceId),
            _dateTimeProvider.UtcNow
        );
        _context.ActivityLogs.Add(auditLog);

        return Result.Success();
    }
}
