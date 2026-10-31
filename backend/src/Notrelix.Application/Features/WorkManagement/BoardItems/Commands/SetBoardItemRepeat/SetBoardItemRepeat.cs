using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.SetBoardItemRepeat;

/// <summary>Sets or (null) clears how an Item repeats. Completing a repeating Item reopens it at the next due date.</summary>
[IdempotencyOperation("work-management.board-items.set-board-item-repeat.v1")]
public record SetBoardItemRepeatCommand(Guid BoardItemId, string? Rule, long ExpectedVersion)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), BoardItemId);
}

public class SetBoardItemRepeatCommandHandler(
    IWorkManagementDbContext context,
    ICurrentRequestContext requestContext,
    IDateTimeProvider clock,
    IBoardRealtimeNotifier? realtime = null) : IRequestHandler<SetBoardItemRepeatCommand, Result>
{
    public async Task<Result> Handle(SetBoardItemRepeatCommand request, CancellationToken ct)
    {
        var response = await HandleCore(request, ct);
        if (realtime is not null) await realtime.NotifyAsync(request, response, ct);
        return response;
    }

    private async Task<Result> HandleCore(SetBoardItemRepeatCommand request, CancellationToken ct)
    {
        var item = await context.BoardItems.FirstOrDefaultAsync(i => i.Id == request.BoardItemId && i.DeletedAt == null, ct)
            ?? throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        item.SetRepeat(request.Rule, requestContext.UserId, clock.UtcNow);
        return Result.Success();
    }
}
