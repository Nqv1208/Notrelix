using MediatR;
using Notrelix.Application.Features.Boards.DTOs;

namespace Notrelix.Application.Features.Boards.Queries.GetFullBoard;

public record GetFullBoardQuery(Guid BoardId) : IRequest<FullBoardDto>;
