using MediatR;
using Notrelix.Application.Features.Boardss.DTOs;

namespace Notrelix.Application.Features.Boardss.Queries.GetFullBoard;

public record GetFullBoardQuery(Guid BoardId) : IRequest<FullBoardDto>;
