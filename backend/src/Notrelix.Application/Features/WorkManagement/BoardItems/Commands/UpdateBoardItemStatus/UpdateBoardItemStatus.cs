using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItemStatus;

[IdempotencyOperation("work-management.board-items.update-board-item-status.v1")]
public record UpdateBoardItemStatusCommand(Guid BoardItemId, string Status, long ExpectedVersion) : ICommand<Result>, IWriteRequest, IIdempotentRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), BoardItemId);
}

public class UpdateBoardItemStatusCommandHandler : IRequestHandler<UpdateBoardItemStatusCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _timeProvider;

    public UpdateBoardItemStatusCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider timeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(UpdateBoardItemStatusCommand request, CancellationToken ct)
    {
        var card = await _context.BoardItems
            // The value collection and the field options are loaded: an existing value
            // is updated in place (unique per item/field) and option-backed values are
            // validated against the persisted options (SPEC WM-V3-GAP-032).
            .Include(c => c.FieldValues)
            .FirstOrDefaultAsync(c => c.Id == request.BoardItemId, ct);
        if (card is null) throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        // The same Status field the board read surfaces (first by position, then id).
        var statusField = await BoardItemFieldWrites.LoadStatusFieldAsync(_context, card.BoardId, ct);
        if (statusField == null)
            return Result.Failure(new ApplicationError("work.board-item.no-status-field", "No status field found on this board.", ApplicationErrorType.BusinessRule));

        var now = _timeProvider.UtcNow;
        var fieldValue = FieldValue.Create(JsonValue.Create(System.Text.Json.JsonSerializer.Serialize(request.Status)));

        BoardItemFieldWrites.Write(card, statusField, fieldValue, statusField, _currentUser.UserId, now);

        return Result.Success();
    }
}
