using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Security;
using Notrelix.Application.Features.WorkManagement.DTOs;
using Notrelix.Domain.WorkManagement;

namespace Notrelix.Application.Features.WorkManagement.Commands;

public record CreateBoardItemCommand(
    Guid WorkspaceId,
    Guid BoardId,
    Guid GroupId,
    string Title,
    double Position) : IRequest<BoardItemSlimDto>, IAuthorizeableRequest
{
    public ResourceType ResourceType => ResourceType.Board;
    public Guid ResourceId => BoardId;
    public PermissionAction Action => PermissionAction.CreateItem;
}

public class CreateBoardItemCommandHandler : IRequestHandler<CreateBoardItemCommand, BoardItemSlimDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public CreateBoardItemCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<BoardItemSlimDto> Handle(CreateBoardItemCommand request, CancellationToken cancellationToken)
    {
        var group = await _context.BoardGroups
            .FirstOrDefaultAsync(g => g.Id == request.GroupId && g.BoardId == request.BoardId, cancellationToken);

        if (group == null)
            throw new NotFoundException("BoardGroup", request.GroupId);

        var item = BoardItem.Create(request.GroupId, request.BoardId, request.WorkspaceId, _currentUser.UserId, request.Title, request.Position);

        _context.BoardItems.Add(item);
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
            new List<Guid>(),
            new List<Guid>()
        );
    }
}
