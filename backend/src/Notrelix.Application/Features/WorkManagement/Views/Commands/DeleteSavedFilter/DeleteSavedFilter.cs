using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Views.Commands.DeleteSavedFilter;

[IdempotencyOperation("work-management.views.delete-saved-filter.v1")]
public record DeleteSavedFilterCommand(Guid FilterId, long ExpectedVersion)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.saved-filter"), FilterId);
    long IExpectedVersionRequest.ExpectedVersion => ExpectedVersion;
    ResourceRef IExpectedVersionRequest.Resource => Resource;
}

public class DeleteSavedFilterCommandHandler : IRequestHandler<DeleteSavedFilterCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeleteSavedFilterCommandHandler(IWorkManagementDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(DeleteSavedFilterCommand request, CancellationToken ct)
    {
        var filter = await _context.SavedFilters
            .FirstOrDefaultAsync(f => f.Id == request.FilterId, ct);
        if (filter is null) throw new NotFoundException("SavedFilter", request.FilterId);

        filter.Delete(_requestContext.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
