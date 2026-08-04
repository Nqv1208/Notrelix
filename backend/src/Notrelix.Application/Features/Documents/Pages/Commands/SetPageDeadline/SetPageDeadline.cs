using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.Documents.Pages.Commands.SetPageDeadline;

public record SetPageDeadlineCommand(Guid PageId, DateTime? Deadline) : ICommand<Result>, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("documents.page"), PageId);
}

public class SetPageDeadlineCommandHandler : IRequestHandler<SetPageDeadlineCommand, Result>
{
    public Task<Result> Handle(SetPageDeadlineCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
