using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Ordering;

namespace Notrelix.Application.Features.WorkManagement.Checklists.Commands.CreateChecklist;

/// <summary>
/// Creates a checklist on a board item, placed relative to the requested neighbors
/// (append when both are omitted) under the item's ItemChecklists ordering lock.
/// </summary>
[IdempotencyOperation("work-management.checklists.create-checklist.v1")]
public record CreateChecklistCommand(Guid BoardItemId, string Title, Guid? PreviousSiblingId = null, Guid? NextSiblingId = null)
    : ICommand<Result<Guid>>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), BoardItemId);
}

public class CreateChecklistCommandHandler : IRequestHandler<CreateChecklistCommand, Result<Guid>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly WorkPlacementResolver _placement;
    private readonly IBoardRealtimeNotifier? _realtime;

    public CreateChecklistCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        WorkPlacementResolver placement,
        IBoardRealtimeNotifier? realtime = null)
    {
        _realtime = realtime;
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _placement = placement;
    }

    public async Task<Result<Guid>> Handle(CreateChecklistCommand request, CancellationToken ct)
    {
        var response = await HandleCore(request, ct);
        if (_realtime is not null) await _realtime.NotifyAsync(request, response, ct);
        return response;
    }

    private async Task<Result<Guid>> HandleCore(CreateChecklistCommand request, CancellationToken ct)
    {
        var item = await _context.BoardItems
            .FirstOrDefaultAsync(i => i.Id == request.BoardItemId, ct)
            ?? throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        // An archived item is read-only: it refuses a new checklist like every other edit.
        if (item.IsArchived)
            throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Item_CannotModifyArchived, "Cannot modify an archived board item.");

        var position = await _placement.ResolveAsync(
            new WorkOrderingScope(WorkOrderingScopeKind.ItemChecklists, item.Id),
            new WorkPlacementIntent(request.PreviousSiblingId, request.NextSiblingId),
            token => ItemChecklistSiblings.LoadAsync(_context, item.Id, token),
            ct);

        var checklist = Checklist.Create(
            _requestContext.RequireAccountId(), item.WorkspaceId, item.Id, request.Title, position,
            _requestContext.UserId, _dateTimeProvider.UtcNow);
        _context.Checklists.Add(checklist);
        return Result<Guid>.Success(checklist.Id);
    }
}
