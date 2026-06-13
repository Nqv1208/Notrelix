using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Security;
using Notrelix.Application.Features.WorkManagement.DTOs;
using Notrelix.Domain.WorkManagement.BoardGroups;

namespace Notrelix.Application.Features.WorkManagement.Commands;

public record MoveBoardItemCommand(
    Guid WorkspaceId,
    Guid BoardId,
    Guid ItemId,
    Guid NewGroupId,
    double Position) : IRequest<BoardItemSlimDto>, IAuthorizeableRequest
{
    public ResourceType ResourceType => ResourceType.Board;
    public Guid ResourceId => BoardId;
    public PermissionAction Action => PermissionAction.MoveItem;
}

public class MoveBoardItemCommandHandler : IRequestHandler<MoveBoardItemCommand, BoardItemSlimDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public MoveBoardItemCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<BoardItemSlimDto> Handle(MoveBoardItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _context.BoardItems
            .Include(item => item.Members)
            .Include(item => item.Labels)
            .FirstOrDefaultAsync(item => item.Id == request.ItemId, cancellationToken);

        if (item == null)
            throw new NotFoundException("BoardItem", request.ItemId);

        var group = await _context.BoardGroups
            .FirstOrDefaultAsync(g => g.Id == request.NewGroupId && g.BoardId == request.BoardId, cancellationToken);

        if (group == null)
            throw new NotFoundException("BoardGroup", request.NewGroupId);

        item.MoveToGroup(BoardGroupRef.From(group), request.Position, _currentUser.UserId);

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
