using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Interfaces;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Boards.Commands.Boards;
using global::Notrelix.Application.Features.Boards.Commands.Checklists.CreateChecklist;
using global::Notrelix.Application.Features.Boards.Commands.Checklists;
using global::Notrelix.Application.Features.Boards.DTOs;
using global::Notrelix.Domain.Common.Exceptions;
using global::Notrelix.Domain.Entities.Boards;
using global::Notrelix.Domain.Entities.Document;
using global::Notrelix.Domain.Entities.Identity;
using global::Notrelix.Domain.Entities.Shared;
using global::Notrelix.Domain.Entities.Workspaces;
using global::Notrelix.Domain.Enums;

namespace Notrelix.Application.Features.Boards.Commands.Checklists.CreateChecklist;

public record CreateChecklistCommand(Guid CardId, string Title) : IRequest<Result<Guid>>;

public class CreateChecklistCommandHandler : IRequestHandler<CreateChecklistCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    public CreateChecklistCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<Guid>> Handle(CreateChecklistCommand request, CancellationToken ct)
    {
        var position = await _context.Checklists
            .Where(c => c.CardId == request.CardId)
            .MaxAsync(c => (double?)c.Position, ct) + 1 ?? 0;

        var checklist = Checklist.Create(request.CardId, request.Title, position);
        _context.Checklists.Add(checklist);
        await _context.SaveChangesAsync(ct);
        return Result<Guid>.Success(checklist.Id);
    }
}
