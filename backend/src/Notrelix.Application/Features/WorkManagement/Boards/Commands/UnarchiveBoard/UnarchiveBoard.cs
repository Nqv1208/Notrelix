using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Boards.Commands.UnarchiveBoard;

public record UnarchiveBoardCommand(Guid BoardId) : ICommand<Result>, ITransactionalRequest;

public class UnarchiveBoardCommandHandler : IRequestHandler<UnarchiveBoardCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UnarchiveBoardCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        IWorkspacePermissionService permissions,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _permissions = permissions;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(UnarchiveBoardCommand request, CancellationToken ct)
    {
        var board = await _context.Boards.FirstOrDefaultAsync(b => b.Id == request.BoardId, ct);
        if (board is null) throw new NotFoundException(nameof(BoardEntity), request.BoardId);
        await _permissions.EnsureCanManageBoardAsync(board.Id, _currentUser.UserId, ct);
        board.Unarchive(_currentUser.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
