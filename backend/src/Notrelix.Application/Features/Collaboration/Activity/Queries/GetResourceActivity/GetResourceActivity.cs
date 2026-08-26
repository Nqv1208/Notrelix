using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Collaboration.Activity.DTOs;

namespace Notrelix.Application.Features.Collaboration.Activity.Queries.GetResourceActivity;

public record GetResourceActivityQuery(ResourceKind ResourceKind, Guid ResourceId, int Page = 1, int PageSize = 20)
    : IQuery<Result<object>>, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest, IRequirePermission
{
    public static GetResourceActivityQuery ForBoardItem(Guid boardItemId, int page = 1, int pageSize = 20)
        => new(ResourceKind.Create(BoardItemKind), boardItemId, page, pageSize);

    private const string BoardItemKind = "work-management.board-item";

    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind, ResourceId);
}

public class GetResourceActivityQueryHandler : IRequestHandler<GetResourceActivityQuery, Result<object>>
{
    public Task<Result<object>> Handle(GetResourceActivityQuery request, CancellationToken ct)
    {
        return Task.FromResult(Result<object>.Success(new
        {
            data = new List<ActivityLogDto>(),
            total = 0,
            page = request.Page,
            pageSize = request.PageSize
        }));
    }
}
