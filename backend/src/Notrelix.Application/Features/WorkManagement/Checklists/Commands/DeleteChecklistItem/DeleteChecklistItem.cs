using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Checklists.Commands.DeleteChecklistItem;

public record DeleteChecklistItemCommand(Guid ItemId) : ICommand<Result>, ITransactionalRequest;

public class DeleteChecklistItemCommandHandler : IRequestHandler<DeleteChecklistItemCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    public DeleteChecklistItemCommandHandler(IWorkManagementDbContext context) => _context = context;

    public async Task<Result> Handle(DeleteChecklistItemCommand request, CancellationToken ct)
    {
        var item = await _context.ChecklistItems.FirstOrDefaultAsync(i => i.Id == request.ItemId, ct);
        if (item is null) throw new NotFoundException(nameof(ChecklistItem), request.ItemId);
        _context.ChecklistItems.Remove(item);
        return Result.Success();
    }
}
