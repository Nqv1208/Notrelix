using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItemFieldValue;

[IdempotencyOperation("work-management.board-items.update-board-item-field-value.v1")]
public record UpdateBoardItemFieldValueCommand(
    Guid ItemId,
    Guid FieldId,
    object? Value,
    long ExpectedVersion) : ICommand<BoardItemSlimDto>, IWriteRequest, IRequirePermission, IAuthenticatedRequest, IResourceScopedRequest, IIdempotentRequest, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), ItemId);
}

public class UpdateBoardItemFieldValueCommandHandler : IRequestHandler<UpdateBoardItemFieldValueCommand, BoardItemSlimDto>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _timeProvider;
    private readonly IBoardRealtimeNotifier? _realtime;
    private readonly IActorLookupService? _actors;
    private readonly ICurrentRequestContext? _requestContext;

    public UpdateBoardItemFieldValueCommandHandler(IWorkManagementDbContext context, ICurrentUser currentUser, IDateTimeProvider timeProvider, IBoardRealtimeNotifier? realtime = null, IActorLookupService? actors = null, ICurrentRequestContext? requestContext = null)
    {
        _actors = actors;
        _requestContext = requestContext;
        _realtime = realtime;
        _context = context;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<BoardItemSlimDto> Handle(UpdateBoardItemFieldValueCommand request, CancellationToken cancellationToken)
    {
        var response = await HandleCore(request, cancellationToken);
        if (_realtime is not null) await _realtime.NotifyAsync(request, response, cancellationToken);
        return response;
    }

    private async Task<BoardItemSlimDto> HandleCore(UpdateBoardItemFieldValueCommand request, CancellationToken cancellationToken)
    {
        var item = await _context.BoardItems
            // The value collection and the field options are loaded: an existing value
            // is updated in place (unique per item/field) and option-backed values are
            // validated against the persisted options (SPEC WM-V3-GAP-032).
            .Include(i => i.FieldValues)
            .FirstOrDefaultAsync(item => item.Id == request.ItemId, cancellationToken);

        if (item == null)
            throw new NotFoundException("BoardItem", request.ItemId);

        var field = await _context.BoardFields
            .Include(f => f.Options)
            .FirstOrDefaultAsync(f => f.Id == request.FieldId && f.BoardId == item.BoardId, cancellationToken);

        if (field == null)
            throw _requestContext is null
                ? new NotFoundException("BoardField", request.FieldId)
                : await BoardFields.StaleFieldGuard.MissingAsync(_context, _actors, _requestContext, item.BoardId, request.FieldId, cancellationToken);

        var now = _timeProvider.UtcNow;
        var jsonString = System.Text.Json.JsonSerializer.Serialize(request.Value);
        var fieldValue = FieldValue.Create(JsonValue.Create(jsonString));

        var statusField = await BoardItemFieldWrites.LoadStatusFieldAsync(_context, item.BoardId, cancellationToken);
        BoardItemFieldWrites.Write(item, field, fieldValue, statusField, _currentUser.UserId, now);

        var memberIds = await _context.BoardItemMembers
            .Where(m => m.ItemId == item.Id)
            .Select(m => m.UserId)
            .ToListAsync(cancellationToken);

        var labelIds = await _context.BoardItemLabels
            .Where(l => l.ItemId == item.Id)
            .Select(l => l.LabelId)
            .ToListAsync(cancellationToken);

        var response = new BoardItemSlimDto(
            item.Id,
            item.GroupId,
            item.Name,
            item.Position.Value,
            memberIds,
            labelIds,
            item.Version
        );
        return response;
    }
}
