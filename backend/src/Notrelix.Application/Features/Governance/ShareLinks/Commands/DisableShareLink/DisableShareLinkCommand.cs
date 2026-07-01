using Notrelix.Application.Common.Models;
using SharedKernel = Notrelix.Domain.SharedKernel;

namespace Notrelix.Application.Features.Governance.ShareLinks.Commands.DisableShareLink;

public record DisableShareLinkCommand(
    Guid WorkspaceId,
    SharedKernel.ResourceType ResourceType,
    Guid ResourceId,
    Guid ShareLinkId) : ICommand<Result>, IRequirePermission, ITransactionalRequest
{
    PermissionAction IRequirePermission.Action => ResourceType switch
    {
        SharedKernel.ResourceType.Board => PermissionAction.ShareBoardView,
        SharedKernel.ResourceType.Page => PermissionAction.SharePage,
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

        return Result.Success();
    }
}
