using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Boards.Commands.DeleteBoard;

[IdempotencyOperation("work-management.boards.delete-board.v1")]
public record DeleteBoardCommand(Guid BoardId)
    : ICommand<Result>,
      IWriteRequest, IAuthenticatedRequest,
      IResourceScopedRequest,
      IRequirePermission,
      IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public class DeleteBoardCommandHandler : IRequestHandler<DeleteBoardCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeleteBoardCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(DeleteBoardCommand request, CancellationToken ct)
    {
        var board = await _context.Boards.FirstOrDefaultAsync(b => b.Id == request.BoardId, ct);
        if (board is null) throw new NotFoundException(nameof(BoardEntity), request.BoardId);

        board.Delete(_currentUser.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
