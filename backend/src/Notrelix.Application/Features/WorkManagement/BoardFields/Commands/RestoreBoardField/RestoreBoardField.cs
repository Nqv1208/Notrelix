using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardFields.Commands.RestoreBoardField;

/// <summary>The window a deleted field can be restored in.</summary>
public static class BoardFieldTrashPolicy
{
    public static readonly TimeSpan RestoreWindow = TimeSpan.FromDays(30);
}

[IdempotencyOperation("work-management.board-fields.restore-board-field.v1")]
public record RestoreBoardFieldCommand(Guid BoardId, Guid FieldId) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    // Restoring undoes a delete, which is Board management (Manager); the board is the resource because the field is hidden while deleted.
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public class RestoreBoardFieldCommandHandler : IRequestHandler<RestoreBoardFieldCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IBoardRealtimeNotifier? _realtime;

    public RestoreBoardFieldCommandHandler(IWorkManagementDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider, IBoardRealtimeNotifier? realtime = null)
    {
        _realtime = realtime;
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(RestoreBoardFieldCommand request, CancellationToken ct)
    {
        var response = await HandleCore(request, ct);
        if (_realtime is not null) await _realtime.NotifyAsync(request, response, ct);
        return response;
    }

    private async Task<Result> HandleCore(RestoreBoardFieldCommand request, CancellationToken ct)
    {
        // The global filter hides soft-deleted rows and bypassing it drops its tenant predicate, so the scope is re-applied.
        var accountId = _requestContext.RequireAccountId();
        var workspaceId = _requestContext.RequireWorkspaceId();
        var field = await _context.BoardFields
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => f.Id == request.FieldId && f.BoardId == request.BoardId && f.AccountId == accountId && f.WorkspaceId == workspaceId, ct);
        if (field is null) throw new NotFoundException(nameof(BoardField), request.FieldId);
        if (!field.IsDeleted) return Result.Success();

        var now = _dateTimeProvider.UtcNow;
        if (field.DeletedAt is { } deletedAt && now - deletedAt > BoardFieldTrashPolicy.RestoreWindow)
            throw new ConflictException("This field was deleted more than 30 days ago and can no longer be restored.", "work-management.field.restore-window-closed");

        await FieldNameGuard.EnsureNameFreeAsync(_context, field.BoardId, field.Id, field.Name, ct);
        field.Restore(_requestContext.UserId, now);
        return Result.Success();
    }
}
