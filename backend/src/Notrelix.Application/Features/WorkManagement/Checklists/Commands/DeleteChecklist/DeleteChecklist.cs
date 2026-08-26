using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Checklists.Commands.DeleteChecklist;

[IdempotencyOperation("work-management.checklists.delete-checklist.v1")]
public record DeleteChecklistCommand(Guid ChecklistId) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.checklist"), ChecklistId);
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
