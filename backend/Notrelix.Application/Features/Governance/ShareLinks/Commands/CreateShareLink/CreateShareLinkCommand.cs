using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.DTOs;
using Notrelix.Domain.Collaboration.Activity;
using Notrelix.Domain.Common;
using Notrelix.Domain.Governance;
using Notrelix.Domain.Governance.ShareLinks;
using SharedKernel = Notrelix.Domain.SharedKernel;
using System.Text.Json;

namespace Notrelix.Application.Features.Governance.ShareLinks.Commands.CreateShareLink;

public record CreateShareLinkResponse(
    ShareLinkDto ShareLink,
    string RawToken
);

public record CreateShareLinkCommand(
    Guid WorkspaceId,
    string ResourceType,
    Guid ResourceId,
    string Level,
    DateTime? ExpiresAt = null) : ICommand<Result<CreateShareLinkResponse>>, IRequirePermission, ITransactionalRequest
{
    PermissionAction IRequirePermission.Action => Enum.Parse<SharedKernel.ResourceType>(ResourceType, true) switch
    {
        SharedKernel.ResourceType.Board => PermissionAction.ShareBoardView,
        SharedKernel.ResourceType.Page => PermissionAction.SharePage,
        _ => PermissionAction.ManageWorkspace
    };
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(Enum.Parse<SharedKernel.ResourceType>(ResourceType, true), ResourceId, WorkspaceId);
}

    public class CreateShareLinkCommandHandler : IRequestHandler<CreateShareLinkCommand, Result<CreateShareLinkResponse>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUser _currentUser;
        private readonly IDateTimeProvider _dateTimeProvider;

        public CreateShareLinkCommandHandler(
            IApplicationDbContext context,
            ICurrentUser currentUser,
            IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _currentUser = currentUser;
            _dateTimeProvider = dateTimeProvider;
        }

    public async Task<Result<CreateShareLinkResponse>> Handle(
        CreateShareLinkCommand request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<SharedKernel.ResourceType>(request.ResourceType, true, out var resourceType))
        {
            return Result<CreateShareLinkResponse>.Failure("Invalid format for enum parameters.");
        }

        var actorId = _currentUser.UserId;

        // Generate raw secure token
        var rawToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        var tokenHash = ShareLinkTokenHash.Create(rawToken);

        var shareLink = ShareLink.Create(
            request.WorkspaceId,
            resourceType,
            request.ResourceId,
            tokenHash,
            ShareLinkAccessMode.WorkspaceOnly,
            actorId,
            _dateTimeProvider.UtcNow,
            request.ExpiresAt
        );

        _context.ShareLinks.Add(shareLink);

        // Write Audit Log
        var metadata = JsonSerializer.Serialize(new
        {
            level = request.Level,
            expiresAt = request.ExpiresAt,
            shareLinkId = shareLink.Id
        });

        var auditLog = ActivityLog.Record(
            request.WorkspaceId,
            actorId,
            ActivityType.Created,
            SharedKernel.ResourceRef.Create(resourceType, request.ResourceId),
            _dateTimeProvider.UtcNow,
            ActivityMetadata.Create(SharedKernel.JsonValue.Create(metadata))
        );
        _context.ActivityLogs.Add(auditLog);

        var dto = new ShareLinkDto(
            shareLink.Id,
            shareLink.WorkspaceId,
            shareLink.ResourceType.ToString(),
            shareLink.ResourceId,
            shareLink.TokenHash.Hash,
            shareLink.AccessMode.ToString(),
            shareLink.Status == ShareLinkStatus.Active,
            shareLink.ExpiresAt);

        return Result<CreateShareLinkResponse>.Success(new CreateShareLinkResponse(dto, rawToken));
    }

}
