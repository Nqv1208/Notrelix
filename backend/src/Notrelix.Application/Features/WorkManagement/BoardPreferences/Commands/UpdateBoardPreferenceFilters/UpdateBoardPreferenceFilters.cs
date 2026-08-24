using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardPreferences.Commands.UpdateBoardPreferenceFilters;

public record UpdateBoardPreferenceFiltersCommand(
    Guid BoardId,
    Guid ViewId,
    List<FilterRule> Filters)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public class UpdateBoardPreferenceFiltersCommandHandler : IRequestHandler<UpdateBoardPreferenceFiltersCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateBoardPreferenceFiltersCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(UpdateBoardPreferenceFiltersCommand request, CancellationToken ct)
    {
        var userId = _requestContext.UserId;

        var preference = await _context.BoardViewUserPreferences
            .FirstOrDefaultAsync(p => p.BoardId == request.BoardId && p.ViewId == request.ViewId && p.UserId == userId, ct);

        if (preference is null)
            throw new NotFoundException("BoardViewUserPreference", $"{request.BoardId}:{request.ViewId}:{userId}");

        preference.ApplyFilter(request.Filters, _dateTimeProvider.UtcNow);

        return Result.Success();
    }
}
