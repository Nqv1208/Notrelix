using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Interfaces;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Boards.Commands.Boards;
using global::Notrelix.Application.Features.Boards.Commands.CardLinks.CreateCardLink;
using global::Notrelix.Application.Features.Boards.Commands.CardLinks;
using global::Notrelix.Application.Features.Boards.Commands.Cards.CreateCard;
using global::Notrelix.Application.Features.Boards.DTOs;
using global::Notrelix.Application.Features.Document.DTOs;
using global::Notrelix.Application.Features.Workspaces.DTOs;
using global::Notrelix.Domain.Common.Exceptions;
using global::Notrelix.Domain.Entities.Boards;
using global::Notrelix.Domain.Entities.Workspaces;
using global::Notrelix.Domain.Enums;

namespace Notrelix.Application.Features.Boards.Commands.CardLinks.CreateCardLink;

public record CreateCardLinkCommand(Guid SourceCardId, Guid TargetCardId, string LinkType) : IRequest<Result<Guid>>;

public class CreateCardLinkCommandHandler : IRequestHandler<CreateCardLinkCommand, Result<Guid>>
{
    public Task<Result<Guid>> Handle(CreateCardLinkCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
