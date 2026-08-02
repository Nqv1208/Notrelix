using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Checklists.Commands.ToggleChecklistItem;

[IdempotencyOperation("work-management.checklists.toggle-checklist-item.v1")]
public record ToggleChecklistItemCommand(Guid ChecklistItemId, string? IdempotencyKey = null) : ICommand<Result>, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.ChecklistItem, ChecklistItemId);
    string IIdempotentRequest.IdempotencyKey => IdempotencyKey ?? $"toggle-checklist-item:{ChecklistItemId}";
}

public class ToggleChecklistItemCommandHandler : IRequestHandler<ToggleChecklistItemCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ToggleChecklistItemCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ToggleChecklistItemCommand request, CancellationToken ct)
    {
        var checklist = await _context.Checklists
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Items.Any(i => i.Id == request.ChecklistItemId), ct);

        if (checklist is null)
            throw new NotFoundException(nameof(ChecklistItem), request.ChecklistItemId);

        var now = _dateTimeProvider.UtcNow;
        checklist.ToggleItem(request.ChecklistItemId, _currentUser.UserId, now);
        return Result.Success();
    }
}
