using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Commands.Boards;
using global::Notrelix.Application.Features.WorkManagement.Commands.Checklists.UpdateChecklist;
using global::Notrelix.Application.Features.WorkManagement.Commands.Checklists.UpdateChecklistItem;
using global::Notrelix.Application.Features.WorkManagement.Commands.Checklists;
using global::Notrelix.Application.Features.WorkManagement.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.WorkManagement.Commands.Checklists.UpdateChecklistItem;

public record UpdateChecklistItemCommand(Guid ItemId, string? Title, bool? IsChecked, DateTime? DueDate, Guid? AssigneeId) : IRequest<Result>;

public class UpdateChecklistItemCommandHandler : IRequestHandler<UpdateChecklistItemCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public UpdateChecklistItemCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(UpdateChecklistItemCommand request, CancellationToken ct)
    {
        var item = await _context.ChecklistItems.FirstOrDefaultAsync(i => i.Id == request.ItemId, ct);
        if (item is null) throw new NotFoundException(nameof(ChecklistItem), request.ItemId);
        if (request.Title is not null) item.UpdateTitle(request.Title);
        if (request.IsChecked.HasValue) { if (request.IsChecked.Value) item.Check(); else item.Uncheck(); }
        if (request.DueDate.HasValue) item.SetDueDate(request.DueDate);
        if (request.AssigneeId.HasValue) item.Assign(request.AssigneeId);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
