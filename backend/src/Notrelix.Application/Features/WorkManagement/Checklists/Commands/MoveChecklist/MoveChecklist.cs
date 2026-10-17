using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Ordering;

namespace Notrelix.Application.Features.WorkManagement.Checklists.Commands.MoveChecklist;

/// <summary>
/// Moves one checklist relative to its neighbors under the board item's
/// ItemChecklists ordering lock (WM-V3-APP-CHK-001); its current place is a no-op.
/// </summary>
[IdempotencyOperation("work-management.checklists.move-checklist.v1")]
public record MoveChecklistCommand(Guid ChecklistId, long ExpectedVersion, Guid? PreviousSiblingId = null, Guid? NextSiblingId = null)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.checklist"), ChecklistId);
}

public class MoveChecklistCommandHandler : IRequestHandler<MoveChecklistCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly WorkPlacementResolver _placement;

    public MoveChecklistCommandHandler(
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

    public async Task<Result> Handle(MoveChecklistCommand request, CancellationToken ct)
    {
        var checklist = await _context.Checklists.FirstOrDefaultAsync(c => c.Id == request.ChecklistId, ct)
            ?? throw new NotFoundException(nameof(Checklist), request.ChecklistId);

        var position = await _placement.ResolveAsync(
            new WorkOrderingScope(WorkOrderingScopeKind.ItemChecklists, checklist.ItemId),
            new WorkPlacementIntent(request.PreviousSiblingId, request.NextSiblingId),
            token => ItemChecklistSiblings.LoadAsync(_context, checklist.ItemId, token),
            ct,
            movedEntityId: checklist.Id);

        checklist.UpdatePosition(position, _currentUser.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
