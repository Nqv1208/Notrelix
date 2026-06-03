using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Interfaces;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Boards.Commands.Boards;
using global::Notrelix.Application.Features.Boards.Commands.Checklists.DeleteChecklist;
using global::Notrelix.Application.Features.Boards.Commands.Checklists;
using global::Notrelix.Application.Features.Boards.DTOs;
using global::Notrelix.Domain.Common.Exceptions;
using global::Notrelix.Domain.Entities.Boards;
using global::Notrelix.Domain.Entities.Document;
using global::Notrelix.Domain.Entities.Identity;
using global::Notrelix.Domain.Entities.Shared;
using global::Notrelix.Domain.Entities.Workspaces;
using global::Notrelix.Domain.Enums;

namespace Notrelix.Application.Features.Boards.Commands.Checklists.DeleteChecklist;

public record DeleteChecklistCommand(Guid ChecklistId) : IRequest<Result>;

public class DeleteChecklistCommandHandler : IRequestHandler<DeleteChecklistCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public DeleteChecklistCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(DeleteChecklistCommand request, CancellationToken ct)
    {
        var checklist = await _context.Checklists.FirstOrDefaultAsync(c => c.Id == request.ChecklistId, ct);
        if (checklist is null) throw new NotFoundException(nameof(Checklist), request.ChecklistId);
        // Delete items first
        var items = await _context.ChecklistItems.Where(i => i.ChecklistId == checklist.Id).ToListAsync(ct);
        _context.ChecklistItems.RemoveRange(items);
        _context.Checklists.Remove(checklist);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
