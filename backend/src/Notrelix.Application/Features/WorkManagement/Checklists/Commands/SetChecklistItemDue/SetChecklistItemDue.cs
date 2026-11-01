using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Checklists.Commands.SetChecklistItemDue;

/// <summary>Sets the due time of one checklist item ("Đặt hạn cho mục"), or clears it (null).</summary>
[IdempotencyOperation("work-management.checklists.set-checklist-item-due.v1")]
public record SetChecklistItemDueCommand(Guid ItemId, DateTimeOffset? DueAt, long ExpectedVersion)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.checklist-item"), ItemId);
}

public class SetChecklistItemDueCommandHandler(
    IWorkManagementDbContext context,
    ICurrentUser currentUser,
    IDateTimeProvider clock,
    IBoardRealtimeNotifier? realtime = null) : IRequestHandler<SetChecklistItemDueCommand, Result>
{
    public async Task<Result> Handle(SetChecklistItemDueCommand request, CancellationToken ct)
    {
        var response = await HandleCore(request, ct);
        if (realtime is not null) await realtime.NotifyAsync(request, response, ct);
        return response;
    }

    private async Task<Result> HandleCore(SetChecklistItemDueCommand request, CancellationToken ct)
    {
        var checklist = await ChecklistAggregateLoader.LoadOwningAsync(context, request.ItemId, ct);
        checklist.SetItemDue(request.ItemId, request.DueAt?.ToUniversalTime(), currentUser.UserId, clock.UtcNow);
        return Result.Success();
    }
}
