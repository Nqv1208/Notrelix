using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.Documents.Pages.Commands.PublishPage;

public record PublishPageCommand(Guid PageId) : ICommand<Result>, IAuthenticatedRequest, INoDataRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("documents.page"), PageId);
}

public class PublishPageCommandHandler : IRequestHandler<PublishPageCommand, Result>
{
    public Task<Result> Handle(PublishPageCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
