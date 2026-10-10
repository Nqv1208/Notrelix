using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Checklists.Commands.UpdateChecklistItem;

/// <summary>
/// Desired-state PATCH of a checklist item (WM-V3-APP-CHK-001): an omitted field is
/// unchanged, <c>IsChecked</c> sets completion (never toggles) and repeating the
/// current state is a no-op. Runs through the owning Checklist aggregate.
/// </summary>
[IdempotencyOperation("work-management.checklists.update-checklist-item.v1")]
public record UpdateChecklistItemCommand(Guid ItemId, string? Title = null, bool? IsChecked = null)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.checklist-item"), ItemId);
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
        var checklist = await ChecklistAggregateLoader.LoadOwningAsync(_context, request.ItemId, ct);
        var now = _dateTimeProvider.UtcNow;

        if (request.Title is not null)
            checklist.RenameItem(request.ItemId, request.Title, _currentUser.UserId, now);
        if (request.IsChecked is { } isChecked)
            checklist.SetItemCompletion(request.ItemId, isChecked, _currentUser.UserId, now);

        return Result.Success();
    }
}
