using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardPreferences.Queries.GetBoardPreference;

public record GetBoardPreferenceQuery(Guid BoardId, Guid ViewId)
    : IQuery<Result<BoardPreferenceDto>>, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    // A person's own settings of a view are decided on the view (a Board child): a view that is not there, or
    // is another person's private one, is the identical NotFound.
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-view"), ViewId);
}

public class GetBoardPreferenceQueryHandler : IRequestHandler<GetBoardPreferenceQuery, Result<BoardPreferenceDto>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;

    public GetBoardPreferenceQueryHandler(IWorkManagementDbContext context, ICurrentRequestContext requestContext)
    {
        _context = context;
        _requestContext = requestContext;
    }

    public async Task<Result<BoardPreferenceDto>> Handle(GetBoardPreferenceQuery request, CancellationToken ct)
    {
        var userId = _requestContext.UserId;

        var dto = await _context.BoardViewUserPreferences
            .AsNoTracking()
            .Where(p => p.BoardId == request.BoardId && p.ViewId == request.ViewId && p.UserId == userId)
            .Select(p => new BoardPreferenceDto(
                p.Id,
                p.BoardId,
                p.ViewId,
                p.FilterRules.ToList(),
                p.SortRules.ToList(),
                p.GroupRule,
                p.Criteria,
                p.Columns))
            .FirstOrDefaultAsync(ct);

        if (dto is null)
            return Result<BoardPreferenceDto>.Success(new BoardPreferenceDto(
                Guid.Empty,
                request.BoardId,
                request.ViewId,
                [],
                [],
                null,
                null,
                null));

        return Result<BoardPreferenceDto>.Success(dto);
    }
}

public record BoardPreferenceDto(
    Guid Id,
    Guid BoardId,
    Guid ViewId,
    List<FilterRule> FilterRules,
    List<SortRule> SortRules,
    GroupRule? GroupRule,
    string? Criteria = null,
    string? Columns = null);

public class GetBoardPreferenceQueryValidator : AbstractValidator<GetBoardPreferenceQuery>
{
    public GetBoardPreferenceQueryValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.ViewId).NotEmpty();
    }
}
