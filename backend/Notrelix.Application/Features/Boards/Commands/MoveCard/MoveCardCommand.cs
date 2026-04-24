using MediatR;

namespace Notrelix.Application.Features.Boardss.Commands.MoveCard;

public record MoveCardCommand(Guid CardId, Guid ListId, double Position) : IRequest<Unit>;
