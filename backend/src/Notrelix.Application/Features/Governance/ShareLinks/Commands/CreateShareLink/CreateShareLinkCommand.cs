using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Governance.DTOs;

namespace Notrelix.Application.Features.Governance.ShareLinks.Commands.CreateShareLink;

public record CreateShareLinkResponse(
    ShareLinkDto ShareLink,
    string RawToken
);

public record CreateShareLinkCommand(
    ResourceType ResourceType,
    Guid ResourceId,
    string Level,
    DateTime? ExpiresAt = null) : ICommand<Result<CreateShareLinkResponse>>, IResourceScopedRequest, IRequirePermission, ITransactionalRequest
{
    PermissionAction IRequirePermission.Action => ResourceType switch
    {
        ResourceType.Board => PermissionAction.ShareBoardView,
        ResourceType.Page => PermissionAction.SharePage,
        _ => PermissionAction.ManageWorkspace
    };
    ResourceRef IResourceScopedRequest.Resource => ResourceRef.Create(ResourceType, ResourceId);
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceType, ResourceId);
}

public class CreateShareLinkCommandHandler : IRequestHandler<CreateShareLinkCommand, Result<CreateShareLinkResponse>>
{
    private readonly IGovernanceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateShareLinkCommandHandler(
        IGovernanceDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public Task<Result<CreateShareLinkResponse>> Handle(
        CreateShareLinkCommand request,
        CancellationToken cancellationToken)
    {
        var actorId = _requestContext.UserId;
        var workspaceId = _requestContext.RequireWorkspaceId();

        var rawToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        var tokenHash = ShareLinkTokenHash.Create(rawToken);

        var shareLink = ShareLink.Create(
            _requestContext.RequireAccountId(),
            workspaceId,
            request.ResourceType,
            request.ResourceId,
            tokenHash,
            ShareLinkAccessMode.WorkspaceOnly,
            actorId,
            _dateTimeProvider.UtcNow,
            request.ExpiresAt
        );

        _context.ShareLinks.Add(shareLink);

        var dto = new ShareLinkDto(
            shareLink.Id,
            shareLink.WorkspaceId,
            shareLink.ResourceType.ToString(),
            shareLink.ResourceId,
            shareLink.TokenHash.Hash,
            shareLink.AccessMode.ToString(),
            shareLink.Status == ShareLinkStatus.Active,
            shareLink.ExpiresAt);

        return Task.FromResult(
            Result<CreateShareLinkResponse>.Success(
                new CreateShareLinkResponse(dto, rawToken)));
    }

}
