using MediatR;

namespace Notrelix.Application.Features.Boardss.Commands.LinkPageToCard;

public record LinkPageToCardCommand(Guid CardId, Guid PageId) : IRequest<Unit>;
