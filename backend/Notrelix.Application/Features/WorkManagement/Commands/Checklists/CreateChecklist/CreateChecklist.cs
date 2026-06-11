using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Commands.Boards;
using global::Notrelix.Application.Features.WorkManagement.Commands.Checklists.CreateChecklist;
using global::Notrelix.Application.Features.WorkManagement.Commands.Checklists;
using global::Notrelix.Application.Features.WorkManagement.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.WorkManagement.Commands.Checklists.CreateChecklist;

public record CreateChecklistCommand(Guid BoardItemId, string Title) : IRequest<Result<Guid>>;

public class CreateChecklistCommandHandler : IRequestHandler<CreateChecklistCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    public CreateChecklistCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<Guid>> Handle(CreateChecklistCommand request, CancellationToken ct)
    {
        var position = await _context.Checklists
            .Where(c => c.BoardItemId == request.BoardItemId)
            .MaxAsync(c => (double?)c.Position, ct) + 1 ?? 0;

        var checklist = Checklist.Create(request.BoardItemId, request.Title, position);
        _context.Checklists.Add(checklist);
        await _context.SaveChangesAsync(ct);
        return Result<Guid>.Success(checklist.Id);
    }
}
