using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Security;
using Notrelix.Application.Features.WorkManagement.DTOs;

namespace Notrelix.Application.Features.WorkManagement.Commands;

public record UpdateBoardItemFieldValueCommand(
    Guid WorkspaceId,
    Guid BoardId,
    Guid ItemId,
    Guid FieldId,
    object? Value) : IRequest<BoardItemSlimDto>, IAuthorizeableRequest
{
    public ResourceType ResourceType => ResourceType.Board;
    public Guid ResourceId => BoardId;
    public PermissionAction Action => PermissionAction.UpdateItem;
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

        await _context.SaveChangesAsync(cancellationToken);

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
