using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Boards.Commands.RestoreBoard;

[IdempotencyOperation("work-management.boards.restore-board.v1")]
public record RestoreBoardCommand(Guid BoardId)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public class RestoreBoardCommandHandler : IRequestHandler<RestoreBoardCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IBoardRealtimeNotifier? _realtime;

    public RestoreBoardCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        IBoardRealtimeNotifier? realtime = null)
    {
        _realtime = realtime;
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(RestoreBoardCommand request, CancellationToken ct)
    {
        var response = await HandleCore(request, ct);
        if (_realtime is not null) await _realtime.NotifyAsync(request, response, ct);
        return response;
    }

    private async Task<Result> HandleCore(RestoreBoardCommand request, CancellationToken ct)
    {
        // The global filter hides soft-deleted rows, and on EF Core 9 bypassing it also
        // drops its tenant predicate, so the resolved tenant scope is re-applied here.
        var accountId = _requestContext.RequireAccountId();
        var workspaceId = _requestContext.RequireWorkspaceId();
        var board = await _context.Boards
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(b => b.Id == request.BoardId && b.AccountId == accountId && b.WorkspaceId == workspaceId, ct);
        if (board is null) throw new NotFoundException(nameof(BoardEntity), request.BoardId);

        board.Restore(_requestContext.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
