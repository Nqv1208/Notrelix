using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.Documents.Pages.Commands.MovePage;

public record MovePageCommand(
    Guid PageId,
    Guid? NewParentId,
    double NewPosition
) : ICommand<Result>, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Page, PageId);
}

public class MovePageCommandHandler : IRequestHandler<MovePageCommand, Result>
{
    public Task<Result> Handle(MovePageCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
