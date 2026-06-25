using MediatR;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Common.DTOs;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Queries.GetMyBoardItems;

public record GetMyBoardItemsQuery(Guid WorkspaceId) : IQuery<Result<List<BoardItemSummaryDto>>>;

public class GetMyBoardItemsQueryHandler : IRequestHandler<GetMyBoardItemsQuery, Result<List<BoardItemSummaryDto>>>
{
    public Task<Result<List<BoardItemSummaryDto>>> Handle(GetMyBoardItemsQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
