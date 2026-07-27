using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItemStatus;

public record UpdateBoardItemStatusCommand(Guid BoardItemId, string Status, string? IdempotencyKey = null) : ICommand<Result>, ITransactionalRequest, IIdempotentRequest
{
    string IIdempotentRequest.IdempotencyKey => IdempotencyKey ?? $"update-item-status:{BoardItemId}";
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
            .FirstOrDefaultAsync(c => c.Id == request.BoardItemId, ct);
        if (card is null) throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        var statusFields = await _context.BoardFields
            .Where(f => f.BoardId == card.BoardId && f.Type == FieldType.Status && !f.IsDeleted)
            .ToListAsync(ct);

        var statusField = statusFields.FirstOrDefault();
        if (statusField == null)
            return Result.Failure("No status field found on this board.");

        var now = _timeProvider.UtcNow;
        var fieldValue = FieldValue.Create(JsonValue.Create(System.Text.Json.JsonSerializer.Serialize(request.Status)));

        card.UpdateFieldValue(statusField, fieldValue, _currentUser.UserId, now);

        return Result.Success();
    }
}
