using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.Documents.Pages.Commands.PublishPage;

public record PublishPageCommand(Guid PageId) : ICommand<Result>;

public class PublishPageCommandHandler : IRequestHandler<PublishPageCommand, Result>
{
    public Task<Result> Handle(PublishPageCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
