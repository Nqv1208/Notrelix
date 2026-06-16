using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Domain.SharedKernel;

namespace Notrelix.Application.Features.WorkManagement.Commands.Checklists.UpdateChecklist;

public record UpdateChecklistCommand(Guid ChecklistId) : IRequest<Result>;

public class UpdateChecklistCommandHandler : IRequestHandler<UpdateChecklistCommand, Result>
{
    private readonly IApplicationDbContext _context;
    public UpdateChecklistCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(UpdateChecklistCommand request, CancellationToken ct)
    {
        var checklist = await _context.Checklists.FirstOrDefaultAsync(c => c.Id == request.ChecklistId, ct);
        if (checklist is null) throw new NotFoundException(nameof(Checklist), request.ChecklistId);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
