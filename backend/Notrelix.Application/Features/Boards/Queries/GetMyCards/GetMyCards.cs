using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Interfaces;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Boards.DTOs;
using global::Notrelix.Application.Features.Document.DTOs;
using global::Notrelix.Application.Features.Workspaces.DTOs;
using global::Notrelix.Domain.Common.Exceptions;
using global::Notrelix.Domain.Entities.Workspaces;

namespace Notrelix.Application.Features.Boards.Queries.GetMyCards;

public record GetMyCardsQuery(Guid WorkspaceId) : IRequest<Result<List<CardSummaryDto>>>;

public class GetMyCardsQueryHandler : IRequestHandler<GetMyCardsQuery, Result<List<CardSummaryDto>>>
{
    public Task<Result<List<CardSummaryDto>>> Handle(GetMyCardsQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
