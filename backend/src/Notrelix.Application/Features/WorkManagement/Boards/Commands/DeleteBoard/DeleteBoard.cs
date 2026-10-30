using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Public.Authorization;

namespace Notrelix.Application.Features.WorkManagement.Boards.Commands.DeleteBoard;

[IdempotencyOperation("work-management.boards.delete-board.v1")]
public record DeleteBoardCommand(Guid BoardId, long ExpectedVersion)
    : ICommand<Result>,
      IWriteRequest, IAuthenticatedRequest,
      IResourceScopedRequest,
      IRequirePermission,
      IExpectedVersionRequest,
      IIdempotentRequest,
      IBoardReachesDeletedRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public class DeleteBoardCommandHandler : IRequestHandler<DeleteBoardCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeleteBoardCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(DeleteBoardCommand request, CancellationToken ct)
    {
        // A deleted board is reachable for its delete, which is then a no-op. The global filter hides it, and
        // on EF Core 9 bypassing the filter also drops its tenant predicate, so the resolved scope is re-applied.
        var accountId = _requestContext.RequireAccountId();
        var workspaceId = _requestContext.RequireWorkspaceId();
        var board = await _context.Boards
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(b => b.Id == request.BoardId && b.AccountId == accountId && b.WorkspaceId == workspaceId, ct);
        if (board is null) throw new NotFoundException(nameof(BoardEntity), request.BoardId);

        board.Delete(_currentUser.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
