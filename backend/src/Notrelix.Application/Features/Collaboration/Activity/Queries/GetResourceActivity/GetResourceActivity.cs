using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.Collaboration.Activity.DTOs;

namespace Notrelix.Application.Features.Collaboration.Activity.Queries.GetResourceActivity;

public record GetResourceActivityQuery(ResourceType ResourceType, Guid ResourceId, int Page = 1, int PageSize = 20)
    : IQuery<Result<object>>, IResourceScopedRequest
{
    public ResourceRef Resource => ResourceRef.Create(ResourceType, ResourceId);
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
