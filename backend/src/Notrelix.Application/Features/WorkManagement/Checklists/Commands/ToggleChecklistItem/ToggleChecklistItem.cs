using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Common.DTOs;
using global::Notrelix.Application.Features.Documents.DTOs;
using global::Notrelix.Application.Features.Workspaces.DTOs;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.WorkManagement.Checklists.Commands.ToggleChecklistItem;

public record ToggleChecklistItemCommand(Guid ChecklistItemId) : ICommand<Result>;

public class ToggleChecklistItemCommandHandler : IRequestHandler<ToggleChecklistItemCommand, Result>
{
    public Task<Result> Handle(ToggleChecklistItemCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
