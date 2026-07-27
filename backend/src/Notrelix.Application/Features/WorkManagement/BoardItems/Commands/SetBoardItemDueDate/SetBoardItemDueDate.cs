using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.SetBoardItemDueDate;

public record SetBoardItemDueDateCommand(Guid BoardItemId, DateTime? DueDate, DateTime? StartDate, string? IdempotencyKey = null) : ICommand<Result>, ITransactionalRequest, IIdempotentRequest
{
    string IIdempotentRequest.IdempotencyKey => IdempotencyKey ?? $"set-item-due-date:{BoardItemId}";
}

public class SetBoardItemDueDateCommandHandler : IRequestHandler<SetBoardItemDueDateCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _timeProvider;

    public SetBoardItemDueDateCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider timeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(SetBoardItemDueDateCommand request, CancellationToken ct)
    {
        var card = await _context.BoardItems
            .FirstOrDefaultAsync(c => c.Id == request.BoardItemId, ct);
        if (card is null) throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        var now = _timeProvider.UtcNow;
        card.SetTimeline(
            request.StartDate.HasValue ? new DateTimeOffset(request.StartDate.Value, TimeSpan.Zero) : card.StartedAt,
            request.DueDate.HasValue ? new DateTimeOffset(request.DueDate.Value, TimeSpan.Zero) : card.DueAt,
            _currentUser.UserId,
            now);

        return Result.Success();
    }
}
