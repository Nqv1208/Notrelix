using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Security;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.DTOs;
using Notrelix.Domain.Common;
using Notrelix.Domain.Governance;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using ResourceTypeEnum = Notrelix.Domain.Governance.Permissions.ResourceType;

namespace Notrelix.Application.Features.Governance.Commands;

public record CreateShareLinkResponse(
    ShareLinkDto ShareLink,
    string RawToken
);

public record CreateShareLinkCommand(
    Guid WorkspaceId,
    string ResourceType,
    Guid ResourceId,
    string Level,
    DateTime? ExpiresAt = null) : IRequest<Result<CreateShareLinkResponse>>, IAuthorizeableRequest
{
    ResourceTypeEnum IAuthorizeableRequest.ResourceType => Enum.Parse<ResourceTypeEnum>(ResourceType, true);
    Guid IAuthorizeableRequest.ResourceId => ResourceId;
    PermissionAction IAuthorizeableRequest.Action => Enum.Parse<ResourceTypeEnum>(ResourceType, true) switch
    {
        ResourceTypeEnum.Board => PermissionAction.ShareBoardView,
        ResourceTypeEnum.Page => PermissionAction.SharePage,
        _ => PermissionAction.ManageWorkspace
    };
}

public class CreateShareLinkCommandHandler : IRequestHandler<CreateShareLinkCommand, Result<CreateShareLinkResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public CreateShareLinkCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<CreateShareLinkResponse>> Handle(
        CreateShareLinkCommand request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ResourceType>(request.ResourceType, true, out var resourceType) ||
            !Enum.TryParse<PermissionLevel>(request.Level, true, out var level))
        {
            return Result<CreateShareLinkResponse>.Failure("Invalid format for enum parameters.");
        }

        var actorId = _currentUser.UserId;

        // Generate raw secure token
        var rawToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        var tokenHash = HashToken(rawToken);

        var shareLink = ShareLink.Create(
            request.WorkspaceId,
            resourceType,
            request.ResourceId,
            tokenHash,
            level,
            request.ExpiresAt,
            actorId
        );

        _context.ShareLinks.Add(shareLink);

        // Write Audit Log
        var metadata = JsonSerializer.Serialize(new
        {
            level = request.Level,
            expiresAt = request.ExpiresAt,
            shareLinkId = shareLink.Id
        });

        var auditLog = ActivityLog.Create(
            request.WorkspaceId,
            actorId,
            "CreateShareLink",
            resourceType,
            request.ResourceId,
            resourceTitle: null,
            metadata: metadata
        );
        _context.ActivityLogs.Add(auditLog);

        await _context.SaveChangesAsync(cancellationToken);

        var dto = new ShareLinkDto(
            shareLink.Id,
            shareLink.WorkspaceId,
            shareLink.ResourceType.ToString(),
            shareLink.ResourceId,
            shareLink.TokenHash,
            shareLink.Level.ToString(),
            shareLink.IsEnabled,
            shareLink.ExpiresAt);

        return Result<CreateShareLinkResponse>.Success(new CreateShareLinkResponse(dto, rawToken));
    }

    private static string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLower();
    }
}
