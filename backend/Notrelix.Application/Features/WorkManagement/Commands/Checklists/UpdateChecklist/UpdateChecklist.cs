using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Commands.Boards;
using global::Notrelix.Application.Features.WorkManagement.Commands.Checklists.UpdateChecklist;
using global::Notrelix.Application.Features.WorkManagement.Commands.Checklists;
using global::Notrelix.Application.Features.WorkManagement.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.WorkManagement.Commands.Checklists.UpdateChecklist;

public record UpdateChecklistCommand(Guid ChecklistId, string? Title, double? Position) : IRequest<Result>;

public class UpdateChecklistCommandHandler : IRequestHandler<UpdateChecklistCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public UpdateChecklistCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(UpdateChecklistCommand request, CancellationToken ct)
    {
        var checklist = await _context.Checklists.FirstOrDefaultAsync(c => c.Id == request.ChecklistId, ct);
        if (checklist is null) throw new NotFoundException(nameof(Checklist), request.ChecklistId);
        if (request.Title is not null) checklist.UpdateTitle(request.Title);
        if (request.Position.HasValue) checklist.UpdatePosition(request.Position.Value);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
