using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Ordering;

namespace Notrelix.Application.Features.WorkManagement.Checklists.Commands.MoveChecklistItem;

/// <summary>
/// Moves one item within its checklist relative to its neighbors, under the
/// checklist's ChecklistItems ordering lock and through the Checklist aggregate
/// (WM-V3-APP-CHK-001); its current place is a semantic no-op.
/// </summary>
[IdempotencyOperation("work-management.checklists.move-checklist-item.v1")]
public record MoveChecklistItemCommand(Guid ItemId, long ExpectedVersion, Guid? PreviousSiblingId = null, Guid? NextSiblingId = null)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.checklist-item"), ItemId);
}

public class MoveChecklistItemCommandHandler : IRequestHandler<MoveChecklistItemCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly WorkPlacementResolver _placement;
    private readonly IBoardRealtimeNotifier? _realtime;

    public MoveChecklistItemCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        WorkPlacementResolver placement,
        IBoardRealtimeNotifier? realtime = null)
    {
        _realtime = realtime;
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _placement = placement;
    }

    public async Task<Result> Handle(MoveChecklistItemCommand request, CancellationToken ct)
    {
        var response = await HandleCore(request, ct);
        if (_realtime is not null) await _realtime.NotifyAsync(request, response, ct);
        return response;
    }

    private async Task<Result> HandleCore(MoveChecklistItemCommand request, CancellationToken ct)
    {
        var checklist = await ChecklistAggregateLoader.LoadOwningAsync(_context, request.ItemId, ct);

        var position = await _placement.ResolveAsync(
            new WorkOrderingScope(WorkOrderingScopeKind.ChecklistItems, checklist.Id),
            new WorkPlacementIntent(request.PreviousSiblingId, request.NextSiblingId),
            token => ChecklistItemSiblings.LoadAsync(_context, checklist.Id, token),
            ct,
            movedEntityId: request.ItemId);

        checklist.MoveItem(request.ItemId, position, _currentUser.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
