using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Security;
using Notrelix.Application.Common.Models;
using Notrelix.Domain.Common;
using Notrelix.Domain.Governance;
using System.Text.Json;

namespace Notrelix.Application.Features.Governance.Commands;

public record DisableShareLinkCommand(
    Guid WorkspaceId,
    ResourceType ResourceType,
    Guid ResourceId,
    Guid ShareLinkId) : IRequest<Result>, IAuthorizeableRequest
{
    ResourceType IAuthorizeableRequest.ResourceType => ResourceType;
    Guid IAuthorizeableRequest.ResourceId => ResourceId;
    PermissionAction IAuthorizeableRequest.Action => ResourceType switch
    {
        ResourceType.Board => PermissionAction.ShareBoardView,
        ResourceType.Page => PermissionAction.SharePage,
        _ => PermissionAction.ManageWorkspace
    };
}

public class DisableShareLinkCommandHandler : IRequestHandler<DisableShareLinkCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public DisableShareLinkCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
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

        shareLink.Disable(userId);

        // Write Audit Log
        var metadata = JsonSerializer.Serialize(new
        {
            shareLinkId = shareLink.Id,
            level = shareLink.Level.ToString()
        });

        var auditLog = ActivityLog.Create(
            shareLink.WorkspaceId,
            userId,
            "DisableShareLink",
            shareLink.ResourceType,
            shareLink.ResourceId,
            resourceTitle: null,
            metadata: metadata
        );
        _context.ActivityLogs.Add(auditLog);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
