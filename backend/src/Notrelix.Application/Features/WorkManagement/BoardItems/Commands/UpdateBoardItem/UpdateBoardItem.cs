using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItem;

public record UpdateBoardItemCommand(Guid BoardItemId, string? Title, string? DescriptionMd, string? Priority, string? Cover, DateTime? DueDate, DateTime? StartDate, long? ExpectedVersion = null) : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), BoardItemId);
    long IExpectedVersionRequest.ExpectedVersion => ExpectedVersion ?? 0;
    ResourceRef IExpectedVersionRequest.Resource => Resource;
}

public class UpdateBoardItemCommandHandler : IRequestHandler<UpdateBoardItemCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _timeProvider;

    public UpdateBoardItemCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider timeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(UpdateBoardItemCommand request, CancellationToken ct)
    {
        var card = await _context.BoardItems
            .FirstOrDefaultAsync(c => c.Id == request.BoardItemId && !c.IsDeleted, ct);
        if (card is null) throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        var now = _timeProvider.UtcNow;

        if (request.Title is not null) card.Rename(request.Title, _currentUser.UserId, now);
        if (request.DueDate.HasValue || request.StartDate.HasValue)
            card.SetTimeline(
                request.StartDate.HasValue ? new DateTimeOffset(request.StartDate.Value, TimeSpan.Zero) : card.StartedAt,
                request.DueDate.HasValue ? new DateTimeOffset(request.DueDate.Value, TimeSpan.Zero) : card.DueAt,
                _currentUser.UserId,
                now);

        return Result.Success();
    }
}
