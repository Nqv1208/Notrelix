using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Common.Idempotency;

namespace Notrelix.Application.Features.WorkManagement.Checklists.Commands.UpdateChecklistItem;

[IdempotencyOperation("work-management.checklists.update-checklist-item.v1")]
public record UpdateChecklistItemCommand(Guid ItemId, bool? IsChecked, string? IdempotencyKey = null) : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.ChecklistItem, ItemId);
    string IIdempotentRequest.IdempotencyKey => IdempotencyKey ?? $"update-checklist-item:{ItemId}";
}

public class UpdateChecklistItemCommandHandler : IRequestHandler<UpdateChecklistItemCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateChecklistItemCommandHandler(IWorkManagementDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(UpdateChecklistItemCommand request, CancellationToken ct)
    {
        var item = await _context.ChecklistItems.FirstOrDefaultAsync(i => i.Id == request.ItemId, ct);
        if (item is null) throw new NotFoundException(nameof(ChecklistItem), request.ItemId);

        if (request.IsChecked.HasValue)
        {
            var checklist = await _context.Checklists.FirstOrDefaultAsync(c => c.Id == item.ChecklistId, ct);
            if (checklist is null) throw new NotFoundException(nameof(Checklist), item.ChecklistId);
            checklist.ToggleItem(request.ItemId, _currentUser.UserId, _dateTimeProvider.UtcNow);
        }

        return Result.Success();
    }
}
