using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Commands.Boards;
using global::Notrelix.Application.Features.WorkManagement.DTOs;
using global::Notrelix.Application.Features.Document.DTOs;
using global::Notrelix.Application.Features.Workspaces.DTOs;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.WorkManagement.Commands;

public record CreateBoardItemLinkCommand(Guid SourceCardId, Guid TargetCardId, string LinkType) : IRequest<Result<Guid>>;

public class CreateBoardItemLinkCommandHandler : IRequestHandler<CreateBoardItemLinkCommand, Result<Guid>>
{
    public Task<Result<Guid>> Handle(CreateBoardItemLinkCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
