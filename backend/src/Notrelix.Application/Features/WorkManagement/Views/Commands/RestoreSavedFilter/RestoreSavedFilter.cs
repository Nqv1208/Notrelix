using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Views.Commands.RestoreSavedFilter;

[IdempotencyOperation("work-management.views.restore-saved-filter.v1")]
public record RestoreSavedFilterCommand(Guid FilterId)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.saved-filter"), FilterId);
}

public class RestoreSavedFilterCommandHandler : IRequestHandler<RestoreSavedFilterCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RestoreSavedFilterCommandHandler(IWorkManagementDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(RestoreSavedFilterCommand request, CancellationToken ct)
    {
        // The global filter hides soft-deleted rows, and on EF Core 9 bypassing it also
        // drops its tenant predicate, so the resolved tenant scope is re-applied here.
        var accountId = _requestContext.RequireAccountId();
        var workspaceId = _requestContext.RequireWorkspaceId();
        var filter = await _context.SavedFilters
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => f.Id == request.FilterId && f.AccountId == accountId && f.WorkspaceId == workspaceId, ct);
        if (filter is null) throw new NotFoundException("SavedFilter", request.FilterId);

        filter.Restore(_requestContext.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
