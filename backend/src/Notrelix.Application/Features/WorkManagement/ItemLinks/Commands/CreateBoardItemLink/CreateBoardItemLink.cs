using MediatR;
using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.WorkManagement.ItemLinks.Commands.CreateBoardItemLink;

public record CreateBoardItemLinkCommand(Guid SourceBoardItemId, Guid TargetBoardItemId, string LinkType) : ICommand<Result<Guid>>;

public class CreateBoardItemLinkCommandHandler : IRequestHandler<CreateBoardItemLinkCommand, Result<Guid>>
{
    public Task<Result<Guid>> Handle(CreateBoardItemLinkCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
