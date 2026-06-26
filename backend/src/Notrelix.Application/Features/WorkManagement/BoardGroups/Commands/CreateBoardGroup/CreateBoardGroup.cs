using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.CreateBoardGroup;

public record CreateBoardGroupCommand(Guid BoardId, string Title, string? Position, string? Color = null) : ICommand<Result<Guid>>, ITransactionalRequest;

public class CreateBoardGroupCommandHandler : IRequestHandler<CreateBoardGroupCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IWorkspacePermissionService _permissions;

    public CreateBoardGroupCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        IWorkspacePermissionService permissions)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _permissions = permissions;
    }

    public async Task<Result<Guid>> Handle(CreateBoardGroupCommand request, CancellationToken ct)
    {
        var board = await _context.Boards
            .FirstOrDefaultAsync(board => board.Id == request.BoardId && !board.IsArchived, ct);
        if (board is null) throw new NotFoundException("Board", request.BoardId);

        await _permissions.EnsureCanEditBoardAsync(request.BoardId, _currentUser.UserId, ct);

        var position = request.Position is not null
            ? FractionalIndex.Create(request.Position)
            : FractionalIndex.Initial();

        var color = request.Color is not null ? Color.Create(request.Color) : Color.Create("#808080");
        var list = BoardGroup.Create(board.WorkspaceId, request.BoardId, request.Title, color, position, _currentUser.UserId, _dateTimeProvider.UtcNow);
        _context.BoardGroups.Add(list);
        return Result<Guid>.Success(list.Id);
    }
}
