using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Checklists.Commands.DeleteChecklist;

/// <summary>
/// Soft-deletes a checklist through its aggregate (WM-V3-APP-CHK-001): one version,
/// <c>ChecklistDeletedDomainEvent</c>, and its items stay owned by the deleted
/// checklist instead of being removed row by row.
/// </summary>
[IdempotencyOperation("work-management.checklists.delete-checklist.v1")]
public record DeleteChecklistCommand(Guid ChecklistId) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.checklist"), ChecklistId);
}

public class DeleteChecklistCommandHandler : IRequestHandler<DeleteChecklistCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IBoardRealtimeNotifier? _realtime;

    public DeleteChecklistCommandHandler(IWorkManagementDbContext context, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider, IBoardRealtimeNotifier? realtime = null)
    {
        _realtime = realtime;
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(DeleteChecklistCommand request, CancellationToken ct)
    {
        var response = await HandleCore(request, ct);
        if (_realtime is not null) await _realtime.NotifyAsync(request, response, ct);
        return response;
    }

    private async Task<Result> HandleCore(DeleteChecklistCommand request, CancellationToken ct)
    {
        var checklist = await _context.Checklists.FirstOrDefaultAsync(c => c.Id == request.ChecklistId, ct)
            ?? throw new NotFoundException(nameof(Checklist), request.ChecklistId);

        checklist.Delete(_currentUser.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
