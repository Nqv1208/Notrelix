using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItemFieldValue;

[IdempotencyOperation("work-management.board-items.update-board-item-field-value.v1")]
public record UpdateBoardItemFieldValueCommand(
    Guid ItemId,
    Guid FieldId,
    object? Value) : ICommand<BoardItemSlimDto>, IWriteRequest, IRequirePermission, IAuthenticatedRequest, IResourceScopedRequest, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), ItemId);
}

public class UpdateBoardItemFieldValueCommandHandler : IRequestHandler<UpdateBoardItemFieldValueCommand, BoardItemSlimDto>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _timeProvider;
    private readonly IRealtimeChangeMapper<UpdateBoardItemFieldValueCommand, BoardItemSlimDto>? _realtime;
    private readonly IIntegrationEventCollector? _events;

    public UpdateBoardItemFieldValueCommandHandler(IWorkManagementDbContext context, ICurrentUser currentUser, IDateTimeProvider timeProvider, IRealtimeChangeMapper<UpdateBoardItemFieldValueCommand, BoardItemSlimDto>? realtime = null, IIntegrationEventCollector? events = null)
    {
        _context = context;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _realtime = realtime;
        _events = events;
    }

    public async Task<BoardItemSlimDto> Handle(UpdateBoardItemFieldValueCommand request, CancellationToken cancellationToken)
    {
        var item = await _context.BoardItems
            .FirstOrDefaultAsync(item => item.Id == request.ItemId, cancellationToken);

        if (item == null)
            throw new NotFoundException("BoardItem", request.ItemId);

        var field = await _context.BoardFields
            .FirstOrDefaultAsync(f => f.Id == request.FieldId && f.BoardId == item.BoardId, cancellationToken);

        if (field == null)
            throw new NotFoundException("BoardField", request.FieldId);

        var now = _timeProvider.UtcNow;
        var jsonString = System.Text.Json.JsonSerializer.Serialize(request.Value);
        var fieldValue = FieldValue.Create(JsonValue.Create(jsonString));

        item.UpdateFieldValue(field, fieldValue, _currentUser.UserId, now);

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
            labelIds
        );
        if (_realtime is not null && _events is not null)
            _events.Add(_realtime.Map(request, response, item.Version));
        return response;
    }
}

public sealed class UpdateBoardItemFieldValueRealtimeMapper(IExecutionContextReader context, IDateTimeProvider time)
    : RealtimeChangeMapper<UpdateBoardItemFieldValueCommand, BoardItemSlimDto>(context, time)
{
    public override RealtimeResourceChangedV1 Map(UpdateBoardItemFieldValueCommand request, BoardItemSlimDto response, long streamVersion) =>
        Create("board", "BoardItem", request.ItemId, "UpdateBoardItemFieldValue", response, streamVersion);
}
