using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.DTOs;
using global::Notrelix.Application.Features.Document.Common;
using global::Notrelix.Application.Features.Document.DTOs;
using global::Notrelix.Application.Features.Workspaces.DTOs;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.Document.Commands.Pages.SetPageDeadline;

public record SetPageDeadlineCommand(Guid PageId, DateTime? Deadline) : IRequest<Result>;

public class SetPageDeadlineCommandHandler : IRequestHandler<SetPageDeadlineCommand, Result>
{
    public Task<Result> Handle(SetPageDeadlineCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
