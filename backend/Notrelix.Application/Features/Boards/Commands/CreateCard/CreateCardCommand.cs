using MediatR;

namespace Notrelix.Application.Features.Boardss.Commands.CreateCard;

public record CreateCardCommand(Guid ListId, string Title) : IRequest<Guid>;
