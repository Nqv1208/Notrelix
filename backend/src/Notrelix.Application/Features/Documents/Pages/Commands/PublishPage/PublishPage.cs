using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.Documents.Pages.Commands.PublishPage;

public record PublishPageCommand(Guid PageId) : ICommand<Result>, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Page, PageId);
}

public class PublishPageCommandHandler : IRequestHandler<PublishPageCommand, Result>
{
    public Task<Result> Handle(PublishPageCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
