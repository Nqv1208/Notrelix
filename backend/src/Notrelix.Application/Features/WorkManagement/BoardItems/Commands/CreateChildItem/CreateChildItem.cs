using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.WorkManagement.Ordering;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.CreateChildItem;

/// <summary>
/// Adds a sub-item under an item. The sub-item lives in its parent's board and group,
/// goes last among the parent's sub-items and starts in the board's default status
/// (the first Status option). Sub-items are one level only.
/// </summary>
[IdempotencyOperation("work-management.board-items.create-child-item.v1")]
public record CreateChildItemCommand(Guid ParentItemId, string Title)
    : ICommand<ChildItemDto>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.CreateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), ParentItemId);
}

public class CreateChildItemCommandHandler : IRequestHandler<CreateChildItemCommand, ChildItemDto>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _timeProvider;
    private readonly WorkPlacementResolver _placement;
    private readonly IBoardItemKeyAllocator _keys;

    public CreateChildItemCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider timeProvider,
        WorkPlacementResolver placement,
        IBoardItemKeyAllocator keys)
    {
        _context = context;
        _requestContext = requestContext;
        _timeProvider = timeProvider;
        _placement = placement;
        _keys = keys;
    }

    public async Task<ChildItemDto> Handle(CreateChildItemCommand request, CancellationToken ct)
    {
        var parent = await _context.BoardItems
            .FirstOrDefaultAsync(item => item.Id == request.ParentItemId && item.DeletedAt == null, ct)
            ?? throw new NotFoundException(nameof(BoardItem), request.ParentItemId);

        var now = _timeProvider.UtcNow;
        var position = await _placement.ResolveAsync(
            new WorkOrderingScope(WorkOrderingScopeKind.ChildItems, parent.Id),
            WorkPlacementIntent.Append,
            token => ChildItemSiblings.LoadAsync(_context, parent.Id, token),
            ct);

        // The key is reserved last, after the ordering lock, so the board row lock
        // is always the innermost lock of the request.
        var key = (await _keys.AllocateAsync(_context, parent.BoardId, 1, ct))[0];

        var child = BoardItem.CreateSubItem(parent, request.Title, position, _requestContext.UserId, now, key.Key, key.Sequence);

        var statusField = await BoardItemFieldWrites.LoadStatusFieldAsync(_context, parent.BoardId, ct);
        var defaultStatus = statusField?.Options.OrderBy(option => option.Position).ThenBy(option => option.Id).FirstOrDefault();
        if (statusField is not null && defaultStatus is not null)
        {
            var value = FieldValue.Create(JsonValue.Create(System.Text.Json.JsonSerializer.Serialize(defaultStatus.Id)));
            BoardItemFieldWrites.Write(child, statusField, value, statusField, _requestContext.UserId, now);
        }

        _context.BoardItems.Add(child);

        return new ChildItemDto(
            child.Id, child.ItemKey, child.Name, child.CompletedAt is not null, child.DueAt,
            null, null, child.Position.Value, child.Version);
    }
}
