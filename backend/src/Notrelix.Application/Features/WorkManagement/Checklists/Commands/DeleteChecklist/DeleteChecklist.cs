using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Checklists.Commands.DeleteChecklist;

public record DeleteChecklistCommand(Guid ChecklistId) : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest
{
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Checklist, ChecklistId);
}

public class DeleteChecklistCommandHandler : IRequestHandler<DeleteChecklistCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    public DeleteChecklistCommandHandler(IWorkManagementDbContext context) => _context = context;

    public async Task<Result> Handle(DeleteChecklistCommand request, CancellationToken ct)
    {
        var checklist = await _context.Checklists.FirstOrDefaultAsync(c => c.Id == request.ChecklistId, ct);
        if (checklist is null) throw new NotFoundException(nameof(Checklist), request.ChecklistId);
        // Delete items first
        var items = await _context.ChecklistItems.Where(i => i.ChecklistId == checklist.Id).ToListAsync(ct);
        _context.ChecklistItems.RemoveRange(items);
        _context.Checklists.Remove(checklist);
        return Result.Success();
    }
}
