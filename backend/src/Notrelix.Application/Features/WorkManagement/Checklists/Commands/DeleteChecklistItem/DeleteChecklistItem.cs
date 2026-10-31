using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Checklists.Commands.DeleteChecklistItem;

/// <summary>Removes an item through the owning Checklist aggregate (WM-V3-APP-CHK-001).</summary>
[IdempotencyOperation("work-management.checklists.delete-checklist-item.v1")]
public record DeleteChecklistItemCommand(Guid ItemId, long ExpectedVersion) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.checklist-item"), ItemId);
}

public class DeleteChecklistItemCommandHandler : IRequestHandler<DeleteChecklistItemCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IBoardRealtimeNotifier? _realtime;

    public DeleteChecklistItemCommandHandler(IWorkManagementDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider, IBoardRealtimeNotifier? realtime = null)
    {
        _realtime = realtime;
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(DeleteChecklistItemCommand request, CancellationToken ct)
    {
        var response = await HandleCore(request, ct);
        if (_realtime is not null) await _realtime.NotifyAsync(request, response, ct);
        return response;
    }

    private async Task<Result> HandleCore(DeleteChecklistItemCommand request, CancellationToken ct)
    {
        var checklist = await ChecklistAggregateLoader.LoadOwningAsync(_context, request.ItemId, ct);
        checklist.RemoveItem(request.ItemId, _currentUser.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
