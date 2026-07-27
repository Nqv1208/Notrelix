using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardPreferences.Commands.CreateOrUpdateBoardPreference;

public record CreateOrUpdateBoardPreferenceCommand(
    Guid BoardId,
    Guid ViewId,
    List<FilterRule>? Filters = null,
    List<SortRule>? Sorts = null,
    GroupRule? Group = null)
    : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Board, BoardId);
}

public class CreateOrUpdateBoardPreferenceCommandHandler : IRequestHandler<CreateOrUpdateBoardPreferenceCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateOrUpdateBoardPreferenceCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(CreateOrUpdateBoardPreferenceCommand request, CancellationToken ct)
    {
        var userId = _requestContext.UserId;
        var now = _dateTimeProvider.UtcNow;

        var existing = await _context.BoardViewUserPreferences
            .FirstOrDefaultAsync(p => p.BoardId == request.BoardId && p.ViewId == request.ViewId && p.UserId == userId, ct);

        if (existing is not null)
        {
            if (request.Filters is not null)
                existing.ApplyFilter(request.Filters, now);

            if (request.Sorts is not null)
                existing.ApplySort(request.Sorts, now);

            existing.ApplyGroup(request.Group, now);

            return Result.Success();
        }

        var preference = BoardViewUserPreference.Create(
            _requestContext.RequireAccountId(),
            _requestContext.RequireWorkspaceId(),
            request.BoardId,
            request.ViewId,
            userId,
            now);

        if (request.Filters is not null)
            preference.ApplyFilter(request.Filters, now);

        if (request.Sorts is not null)
            preference.ApplySort(request.Sorts, now);

        preference.ApplyGroup(request.Group, now);

        _context.BoardViewUserPreferences.Add(preference);

        return Result.Success();
    }
}
