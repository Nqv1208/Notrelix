using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Abstractions;

namespace Notrelix.Application.Features.Governance.ShareLinks.Commands.DisableShareLink;

/// <summary>
/// Turns a share link off. It is authorized on the shared resource named by the route, with the same
/// action that creating a link there needs (a Board manager may turn off a link on their board), so
/// the link must belong to that resource.
/// </summary>
public record DisableShareLinkCommand(
    string ResourceKind,
    Guid ResourceId,
    Guid ShareLinkId) : ICommand<Result>, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IWriteRequest
{
    internal ResourceKind Kind => ShareLinkResources.ParseKind(ResourceKind);

    PermissionAction IRequirePermission.Action => ShareLinkResources.ActionFor(Kind);
    ResourceRef IResourceScopedRequest.Resource => ResourceRef.Create(Kind, ResourceId);
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(Kind, ResourceId);
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

        // A link of another resource answers the same NotFound as a link that does not exist.
        if (shareLink == null || shareLink.ResourceKind != request.Kind || shareLink.ResourceId != request.ResourceId)
        {
            throw new NotFoundException(nameof(ShareLink), request.ShareLinkId);
        }

        var userId = _currentUser.UserId;

        shareLink.Disable(userId, _dateTimeProvider.UtcNow);

        return Result.Success();
    }
}
