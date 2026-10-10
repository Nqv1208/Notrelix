using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Ordering;

namespace Notrelix.Application.Features.WorkManagement.Checklists.Commands.CreateChecklistItem;

/// <summary>
/// Adds an item through the owning Checklist aggregate (WM-V3-APP-CHK-001), placed
/// relative to the requested neighbors under the checklist's ChecklistItems lock.
/// </summary>
[IdempotencyOperation("work-management.checklists.create-checklist-item.v1")]
public record CreateChecklistItemCommand(Guid ChecklistId, string Title, Guid? PreviousSiblingId = null, Guid? NextSiblingId = null)
    : ICommand<Result<Guid>>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.checklist"), ChecklistId);
}

public class CreateChecklistItemCommandHandler : IRequestHandler<CreateChecklistItemCommand, Result<Guid>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly WorkPlacementResolver _placement;

    public CreateChecklistItemCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        WorkPlacementResolver placement)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _placement = placement;
    }

    public async Task<Result<Guid>> Handle(CreateChecklistItemCommand request, CancellationToken ct)
    {
        var checklist = await ChecklistAggregateLoader.LoadAsync(_context, request.ChecklistId, ct);

        var position = await _placement.ResolveAsync(
            new WorkOrderingScope(WorkOrderingScopeKind.ChecklistItems, checklist.Id),
            new WorkPlacementIntent(request.PreviousSiblingId, request.NextSiblingId),
            token => ChecklistItemSiblings.LoadAsync(_context, checklist.Id, token),
            ct);

        var item = checklist.AddItem(request.Title, position, _currentUser.UserId, _dateTimeProvider.UtcNow);
        return Result<Guid>.Success(item.Id);
    }
}
