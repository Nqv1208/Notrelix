using MediatR;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Documents.Pages.Queries.GetPageBreadcrumb;

namespace Notrelix.API.Endpoints.Documents.Pages.Queries;

public static class GetPageBreadcrumbEndpoint
{
    public static IEndpointRouteBuilder MapGetPageBreadcrumb(this IEndpointRouteBuilder group)
    {
        group.MapGet("/breadcrumb", HandleAsync)
            .WithName("Documents.Pages.GetPageBreadcrumb")
            .WithTags("Documents.Pages")
            .WithSummary("Get breadcrumb for a page");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid pageId, ISender sender)
    {
        var result = await sender.Send(new GetPageBreadcrumbQuery(pageId));
        return result.ToApiResult();
    }
}
