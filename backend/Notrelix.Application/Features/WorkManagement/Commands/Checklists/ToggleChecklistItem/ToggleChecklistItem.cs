using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Commands.Boards;
using global::Notrelix.Application.Features.WorkManagement.Commands.Checklists.ToggleChecklistItem;
using global::Notrelix.Application.Features.WorkManagement.Commands.Checklists;
using global::Notrelix.Application.Features.WorkManagement.DTOs;
using global::Notrelix.Application.Features.Document.DTOs;
using global::Notrelix.Application.Features.Workspaces.DTOs;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.WorkManagement.Commands.Checklists.ToggleChecklistItem;

public record ToggleChecklistItemCommand(Guid ChecklistItemId) : IRequest<Result>;

public class ToggleChecklistItemCommandHandler : IRequestHandler<ToggleChecklistItemCommand, Result>
{
    public Task<Result> Handle(ToggleChecklistItemCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
