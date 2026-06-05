using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Interfaces;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Boards.Commands.Boards;
using global::Notrelix.Application.Features.Boards.Commands.Checklists.CreateChecklist;
using global::Notrelix.Application.Features.Boards.Commands.Checklists.CreateChecklistItem;
using global::Notrelix.Application.Features.Boards.Commands.Checklists;
using global::Notrelix.Application.Features.Boards.DTOs;
using global::Notrelix.Domain.Common.Exceptions;
using global::Notrelix.Domain.Entities.Boards;
using global::Notrelix.Domain.Entities.Document;
using global::Notrelix.Domain.Entities.Identity;
using global::Notrelix.Domain.Entities.Shared;
using global::Notrelix.Domain.Entities.Workspaces;
using global::Notrelix.Domain.Enums;

namespace Notrelix.Application.Features.Boards.Commands.Checklists.CreateChecklistItem;

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
