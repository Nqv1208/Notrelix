using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.DTOs;
using global::Notrelix.Application.Features.Document.DTOs;
using global::Notrelix.Application.Features.Workspaces.DTOs;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.WorkManagement.Queries.GetMyCards;

public record GetMyBoardItemsQuery(Guid WorkspaceId) : IRequest<Result<List<BoardItemSummaryDto>>>;

public class GetMyBoardItemsQueryHandler : IRequestHandler<GetMyBoardItemsQuery, Result<List<BoardItemSummaryDto>>>
{
    public Task<Result<List<BoardItemSummaryDto>>> Handle(GetMyBoardItemsQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
