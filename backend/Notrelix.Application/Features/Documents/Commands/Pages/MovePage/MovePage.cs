using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.DTOs;
using global::Notrelix.Application.Features.Document.Common;
using global::Notrelix.Application.Features.Document.DTOs;
using global::Notrelix.Application.Features.Workspaces.DTOs;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.Document.Commands.Pages.MovePage;

public record MovePageCommand(
    Guid PageId,
    Guid? NewParentId,
    double NewPosition
) : IRequest<Result>;

public class MovePageCommandHandler : IRequestHandler<MovePageCommand, Result>
{
    public Task<Result> Handle(MovePageCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
