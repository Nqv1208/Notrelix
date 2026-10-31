using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.WorkManagement.Ordering;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.MoveChildItem;

/// <summary>Places a sub-item among the other sub-items of its parent.</summary>
[IdempotencyOperation("work-management.board-items.move-child-item.v1")]
public record MoveChildItemCommand(
    Guid ItemId,
    long ExpectedVersion,
    Guid? PreviousSiblingId = null,
    Guid? NextSiblingId = null)
    : ICommand<ChildItemDto>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.MoveItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), ItemId);
}

public class MoveChildItemCommandHandler : IRequestHandler<MoveChildItemCommand, ChildItemDto>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _timeProvider;
    private readonly WorkPlacementResolver _placement;
    private readonly IBoardRealtimeNotifier? _realtime;

    public MoveChildItemCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider timeProvider,
        WorkPlacementResolver placement,
        IBoardRealtimeNotifier? realtime = null)
    {
        _realtime = realtime;
        _context = context;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _placement = placement;
    }

    public async Task<ChildItemDto> Handle(MoveChildItemCommand request, CancellationToken ct)
    {
        var response = await HandleCore(request, ct);
        if (_realtime is not null) await _realtime.NotifyAsync(request, response, ct);
        return response;
    }

    private async Task<ChildItemDto> HandleCore(MoveChildItemCommand request, CancellationToken ct)
    {
        var item = await _context.BoardItems
            .FirstOrDefaultAsync(i => i.Id == request.ItemId && i.DeletedAt == null, ct)
            ?? throw new NotFoundException(nameof(BoardItem), request.ItemId);

        if (item.ParentItemId is not { } parentId)
            throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(WorkManagementRuleCodes.WorkManagement_Item_NotASubItem, "Only a sub-item is ordered under a parent.");

        var position = await _placement.ResolveAsync(
            new WorkOrderingScope(WorkOrderingScopeKind.ChildItems, parentId),
            new WorkPlacementIntent(request.PreviousSiblingId, request.NextSiblingId),
            token => ChildItemSiblings.LoadAsync(_context, parentId, token),
            ct,
            movedEntityId: item.Id);

        item.RepositionWithinParent(position, _currentUser.UserId, _timeProvider.UtcNow);

        return new ChildItemDto(
            item.Id, item.ItemKey, item.Name, item.CompletedAt is not null, item.DueAt,
            null, null, item.Position.Value, item.Version);
    }
}
