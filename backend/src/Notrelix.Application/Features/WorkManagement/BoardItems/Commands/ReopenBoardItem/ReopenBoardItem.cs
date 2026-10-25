using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.ReopenBoardItem;

/// <summary>
/// Reopens a completed Item: it returns to the Status it held before it was
/// completed when that is recorded and still open, otherwise to the Board's first
/// status that is not done. Completion (who, when) is cleared.
/// </summary>
[IdempotencyOperation("work-management.board-items.reopen-board-item.v1")]
public record ReopenBoardItemCommand(Guid BoardItemId)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), BoardItemId);
}

public class ReopenBoardItemCommandHandler : IRequestHandler<ReopenBoardItemCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _timeProvider;

    public ReopenBoardItemCommandHandler(IWorkManagementDbContext context, ICurrentUser currentUser, IDateTimeProvider timeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(ReopenBoardItemCommand request, CancellationToken ct)
    {
        var item = await _context.BoardItems
            .Include(i => i.FieldValues)
            .FirstOrDefaultAsync(i => i.Id == request.BoardItemId, ct);
        if (item is null) throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        if (!item.CompletedAt.HasValue) return Result.Success();

        var now = _timeProvider.UtcNow;
        var statusField = await BoardItemFieldWrites.LoadStatusFieldAsync(_context, item.BoardId, ct);
        var target = statusField is null ? null : ResolveTarget(statusField, item.StatusBeforeCompletionOptionId);
        if (statusField is not null && target is not null)
        {
            var value = FieldValue.Create(JsonValue.Create(System.Text.Json.JsonSerializer.Serialize(target.Id)));
            BoardItemFieldWrites.Write(item, statusField, value, statusField, _currentUser.UserId, now);
        }
        else
        {
            item.Reopen(_currentUser.UserId, now);
        }

        return Result.Success();
    }

    /// <summary>The recorded earlier status when it is still an open option, else the first open option.</summary>
    private static FieldOption? ResolveTarget(BoardField statusField, Guid? recorded)
    {
        var open = statusField.Options.Where(option => option.Category != WorkStatusCategory.Done).ToList();
        return open.FirstOrDefault(option => option.Id == recorded)
            ?? open.OrderBy(option => option.Position).ThenBy(option => option.Id).FirstOrDefault();
    }
}
