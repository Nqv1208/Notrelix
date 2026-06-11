using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Commands.Boards;
using global::Notrelix.Application.Features.WorkManagement.Commands.Checklists.CreateChecklist;
using global::Notrelix.Application.Features.WorkManagement.Commands.Checklists.CreateChecklistItem;
using global::Notrelix.Application.Features.WorkManagement.Commands.Checklists;
using global::Notrelix.Application.Features.WorkManagement.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.WorkManagement.Commands.Checklists.CreateChecklistItem;

public record CreateChecklistItemCommand(Guid ChecklistId, string Title) : IRequest<Result<Guid>>;

public class CreateChecklistItemCommandHandler : IRequestHandler<CreateChecklistItemCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    public CreateChecklistItemCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<Guid>> Handle(CreateChecklistItemCommand request, CancellationToken ct)
    {
        var position = await _context.ChecklistItems
            .Where(i => i.ChecklistId == request.ChecklistId)
            .MaxAsync(i => (double?)i.Position, ct) + 1 ?? 0;

        var item = ChecklistItem.Create(request.ChecklistId, request.Title, position);
        _context.ChecklistItems.Add(item);
        await _context.SaveChangesAsync(ct);
        return Result<Guid>.Success(item.Id);
    }
}
