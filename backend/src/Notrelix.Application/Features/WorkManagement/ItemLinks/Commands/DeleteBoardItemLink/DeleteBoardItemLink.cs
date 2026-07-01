using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.WorkManagement.ItemLinks.Commands.DeleteBoardItemLink;

public record DeleteBoardItemLinkCommand(Guid BoardItemLinkId) : ICommand<Result>;

public class DeleteBoardItemLinkCommandHandler : IRequestHandler<DeleteBoardItemLinkCommand, Result>
{
    public Task<Result> Handle(DeleteBoardItemLinkCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
