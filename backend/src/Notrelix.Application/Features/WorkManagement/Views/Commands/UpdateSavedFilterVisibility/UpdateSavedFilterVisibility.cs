using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Views.Commands.UpdateSavedFilterVisibility;

[IdempotencyOperation("work-management.views.update-saved-filter-visibility.v1")]
public record UpdateSavedFilterVisibilityCommand(Guid FilterId, SavedFilterVisibility Visibility, long ExpectedVersion, string? IdempotencyKey = null)
    : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.SavedFilter, FilterId);
    long IExpectedVersionRequest.ExpectedVersion => ExpectedVersion;
    ResourceRef IExpectedVersionRequest.Resource => Resource;
    string IIdempotentRequest.IdempotencyKey => IdempotencyKey ?? $"update-filter-visibility:{FilterId}";
}

public class UpdateSavedFilterVisibilityCommandHandler : IRequestHandler<UpdateSavedFilterVisibilityCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateSavedFilterVisibilityCommandHandler(IWorkManagementDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(UpdateSavedFilterVisibilityCommand request, CancellationToken ct)
    {
        var filter = await _context.SavedFilters
            .FirstOrDefaultAsync(f => f.Id == request.FilterId, ct);
        if (filter is null) throw new NotFoundException("SavedFilter", request.FilterId);

        filter.UpdateVisibility(request.Visibility, _requestContext.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
