using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Views.Commands.UpdateSavedFilterVisibility;

[IdempotencyOperation("work-management.views.update-saved-filter-visibility.v1")]
public record UpdateSavedFilterVisibilityCommand(Guid FilterId, SavedFilterVisibility Visibility, long ExpectedVersion)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.UpdateBoardView;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.saved-filter"), FilterId);
    long IExpectedVersionRequest.ExpectedVersion => ExpectedVersion;
    ResourceRef IExpectedVersionRequest.Resource => Resource;
}

public class UpdateSavedFilterVisibilityCommandHandler : IRequestHandler<UpdateSavedFilterVisibilityCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IBoardRealtimeNotifier? _realtime;

    public UpdateSavedFilterVisibilityCommandHandler(IWorkManagementDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider, IBoardRealtimeNotifier? realtime = null)
    {
        _realtime = realtime;
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(UpdateSavedFilterVisibilityCommand request, CancellationToken ct)
    {
        var response = await HandleCore(request, ct);
        if (_realtime is not null) await _realtime.NotifyAsync(request, response, ct);
        return response;
    }

    private async Task<Result> HandleCore(UpdateSavedFilterVisibilityCommand request, CancellationToken ct)
    {
        var filter = await _context.SavedFilters
            .FirstOrDefaultAsync(f => f.Id == request.FilterId, ct);
        if (filter is null || !SavedFilterAccess.IsVisibleTo(filter, _requestContext.UserId)) throw new NotFoundException("SavedFilter", request.FilterId);

        // Sharing is the creator's call: another editor of a shared filter may change it, not take it back.
        if (filter.CreatedBy != _requestContext.UserId) throw new ForbiddenException("Only the creator can change who sees a saved filter.");

        filter.UpdateVisibility(request.Visibility, _requestContext.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
