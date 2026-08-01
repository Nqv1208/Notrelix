using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.Documents.Pages.Commands.ArchivePage;

public record ArchivePageCommand(Guid PageId) : ICommand<Result>, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Page, PageId);
}

public class ArchivePageCommandHandler : IRequestHandler<ArchivePageCommand, Result>
{
    public Task<Result> Handle(ArchivePageCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
