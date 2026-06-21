using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItemFieldValue;

public record UpdateBoardItemFieldValueCommand(
    Guid WorkspaceId,
    Guid BoardId,
    Guid ItemId,
    Guid FieldId,
    object? Value) : ICommand<BoardItemSlimDto>, ITransactionalRequest, IRequirePermission, IWorkspaceRequest, IRealtimeRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Board, BoardId, WorkspaceId);
    public RealtimeTopic Topic => new("board", "Board", BoardId);
}

public class UpdateBoardItemFieldValueCommandHandler : IRequestHandler<UpdateBoardItemFieldValueCommand, BoardItemSlimDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _timeProvider;

    public UpdateBoardItemFieldValueCommandHandler(IApplicationDbContext context, ICurrentUser currentUser, IDateTimeProvider timeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<BoardItemSlimDto> Handle(UpdateBoardItemFieldValueCommand request, CancellationToken cancellationToken)
    {
        var item = await _context.BoardItems
            .FirstOrDefaultAsync(item => item.Id == request.ItemId, cancellationToken);

        if (item == null)
            throw new NotFoundException("BoardItem", request.ItemId);

        var field = await _context.BoardFields
            .FirstOrDefaultAsync(f => f.Id == request.FieldId && f.BoardId == request.BoardId, cancellationToken);

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

        return new BoardItemSlimDto(
            item.Id,
            item.GroupId,
            item.Name,
            item.Position.Value,
            memberIds,
            labelIds
        );
    }
}
