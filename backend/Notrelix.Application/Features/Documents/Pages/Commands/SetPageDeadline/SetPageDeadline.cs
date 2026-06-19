using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Common.DTOs;
using global::Notrelix.Application.Features.Documents.Common;
using global::Notrelix.Application.Features.Documents.DTOs;
using global::Notrelix.Application.Features.Workspaces.DTOs;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.Documents.Pages.Commands.SetPageDeadline;

public record SetPageDeadlineCommand(Guid PageId, DateTime? Deadline) : ICommand<Result>;

public class SetPageDeadlineCommandHandler : IRequestHandler<SetPageDeadlineCommand, Result>
{
    public Task<Result> Handle(SetPageDeadlineCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
