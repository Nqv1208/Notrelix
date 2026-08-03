using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Views.Commands.UpdateSavedFilterFilters;

[IdempotencyOperation("work-management.views.update-saved-filter-filters.v1")]
public record UpdateSavedFilterFiltersCommand(Guid FilterId, List<FilterRule> Rules, long ExpectedVersion)
    : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.SavedFilter, FilterId);
    long IExpectedVersionRequest.ExpectedVersion => ExpectedVersion;
    ResourceRef IExpectedVersionRequest.Resource => Resource;
}

public class UpdateSavedFilterFiltersCommandHandler : IRequestHandler<UpdateSavedFilterFiltersCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateSavedFilterFiltersCommandHandler(IWorkManagementDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(UpdateSavedFilterFiltersCommand request, CancellationToken ct)
    {
        var filter = await _context.SavedFilters
            .FirstOrDefaultAsync(f => f.Id == request.FilterId, ct);
        if (filter is null) throw new NotFoundException("SavedFilter", request.FilterId);

        filter.UpdateFilters(request.Rules, _requestContext.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
