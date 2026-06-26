using MediatR;
using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.Documents.Pages.Commands.ArchivePage;

public record ArchivePageCommand(Guid PageId) : ICommand<Result>;

public class ArchivePageCommandHandler : IRequestHandler<ArchivePageCommand, Result>
{
    public Task<Result> Handle(ArchivePageCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
