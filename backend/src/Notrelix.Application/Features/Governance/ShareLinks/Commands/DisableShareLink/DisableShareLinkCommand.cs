using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Abstractions;

namespace Notrelix.Application.Features.Governance.ShareLinks.Commands.DisableShareLink;

public record DisableShareLinkCommand(
    Guid ShareLinkId) : ICommand<Result>, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IWriteRequest
{
    PermissionAction IRequirePermission.Action => PermissionAction.ManageWorkspace;
    ResourceRef IResourceScopedRequest.Resource => ResourceRef.Create(ResourceKind.Create("governance.share-link"), ShareLinkId);
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("governance.share-link"), ShareLinkId);
}

public class DisableShareLinkCommandHandler : IRequestHandler<DisableShareLinkCommand, Result>
{
    private readonly IGovernanceDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DisableShareLinkCommandHandler(
        IGovernanceDbContext context,
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

        return Result.Success();
    }
}
