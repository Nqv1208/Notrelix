using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Governance.DTOs;
using SharedKernel = Notrelix.Domain.SharedKernel;

namespace Notrelix.Application.Features.Governance.ShareLinks.Commands.CreateShareLink;

public record CreateShareLinkResponse(
    ShareLinkDto ShareLink,
    string RawToken
);

public record CreateShareLinkCommand(
    Guid WorkspaceId,
    SharedKernel.ResourceType ResourceType,
    Guid ResourceId,
    string Level,
    DateTime? ExpiresAt = null) : ICommand<Result<CreateShareLinkResponse>>, IRequirePermission, ITransactionalRequest
{
    PermissionAction IRequirePermission.Action => ResourceType switch
    {
        SharedKernel.ResourceType.Board => PermissionAction.ShareBoardView,
        SharedKernel.ResourceType.Page => PermissionAction.SharePage,
        _ => PermissionAction.ManageWorkspace
    };
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceType, ResourceId, WorkspaceId);
}

public class CreateShareLinkCommandHandler : IRequestHandler<CreateShareLinkCommand, Result<CreateShareLinkResponse>>
{
    private readonly IGovernanceDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateShareLinkCommandHandler(
        IGovernanceDbContext context,
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
        var actorId = _currentUser.UserId;

        // Generate raw secure token
        var rawToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        var tokenHash = ShareLinkTokenHash.Create(rawToken);

        var shareLink = ShareLink.Create(
            Guid.Empty,
            request.WorkspaceId,
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

        return Result<CreateShareLinkResponse>.Success(new CreateShareLinkResponse(dto, rawToken));
    }

}
