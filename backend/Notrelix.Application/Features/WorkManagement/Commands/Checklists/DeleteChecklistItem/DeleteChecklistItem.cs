using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Commands.Boards;
using global::Notrelix.Application.Features.WorkManagement.Commands.Checklists.DeleteChecklist;
using global::Notrelix.Application.Features.WorkManagement.Commands.Checklists.DeleteChecklistItem;
using global::Notrelix.Application.Features.WorkManagement.Commands.Checklists;
using global::Notrelix.Application.Features.WorkManagement.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.WorkManagement.Commands.Checklists.DeleteChecklistItem;

public record DeleteChecklistItemCommand(Guid ItemId) : IRequest<Result>;

public class DeleteChecklistItemCommandHandler : IRequestHandler<DeleteChecklistItemCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public DeleteChecklistItemCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(DeleteChecklistItemCommand request, CancellationToken ct)
    {
        var item = await _context.ChecklistItems.FirstOrDefaultAsync(i => i.Id == request.ItemId, ct);
        if (item is null) throw new NotFoundException(nameof(ChecklistItem), request.ItemId);
        _context.ChecklistItems.Remove(item);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
