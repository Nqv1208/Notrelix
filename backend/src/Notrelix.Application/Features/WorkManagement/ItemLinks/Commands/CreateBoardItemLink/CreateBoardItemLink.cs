using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Common.DTOs;
using global::Notrelix.Application.Features.Documents.DTOs;
using global::Notrelix.Application.Features.Workspaces.DTOs;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.WorkManagement.ItemLinks.Commands.CreateBoardItemLink;

public record CreateBoardItemLinkCommand(Guid SourceCardId, Guid TargetCardId, string LinkType) : ICommand<Result<Guid>>;

public class CreateBoardItemLinkCommandHandler : IRequestHandler<CreateBoardItemLinkCommand, Result<Guid>>
{
    public Task<Result<Guid>> Handle(CreateBoardItemLinkCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
