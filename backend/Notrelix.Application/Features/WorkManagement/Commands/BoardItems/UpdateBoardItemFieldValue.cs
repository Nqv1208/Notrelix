using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Abstractions;
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

    public UpdateBoardItemFieldValueCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<BoardItemSlimDto> Handle(UpdateBoardItemFieldValueCommand request, CancellationToken cancellationToken)
    {
        var item = await _context.BoardItems
            .Include(item => item.Members)
            .Include(item => item.Labels)
            .FirstOrDefaultAsync(item => item.Id == request.ItemId, cancellationToken);

        if (item == null)
            throw new NotFoundException("BoardItem", request.ItemId);

        var field = await _context.BoardFields
            .FirstOrDefaultAsync(f => f.Id == request.FieldId && f.BoardId == request.BoardId, cancellationToken);

        if (field == null)
            throw new NotFoundException("BoardField", request.FieldId);

        // Gọi domain method để validate và cập nhật giá trị
        item.UpdateFieldValue(request.FieldId, request.Value, field.Type, field.Settings, request.BoardId, _currentUser.UserId);

        await _context.SaveChangesAsync(cancellationToken);

        return new BoardItemSlimDto(
            item.Id,
            item.GroupId,
            item.Title,
            item.DescriptionMd,
            item.Position,
            item.Priority?.ToString(),
            item.Status.ToString(),
            item.DueDate,
            item.StartDate,
            item.ValuesJson,
            item.Members.Select(m => m.UserId).ToList(),
            item.Labels.Select(l => l.LabelId).ToList()
        );
    }
}
