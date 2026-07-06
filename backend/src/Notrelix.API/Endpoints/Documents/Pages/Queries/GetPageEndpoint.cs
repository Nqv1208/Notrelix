using Notrelix.API.Extensions;
using Notrelix.Application.Features.Documents.Pages.Queries.GetPage;

namespace Notrelix.API.Endpoints.Documents.Pages.Queries;

public static class GetPageEndpoint
{
    public static IEndpointRouteBuilder MapGetPage(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/", HandleAsync)
            .WithName("Documents.Pages.GetPage")
            .WithTags("Documents.Pages")
            .WithSummary("Get a page by ID");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid pageId, ISender sender)
    {
        var result = await sender.Send(new GetPageQuery(pageId));
        return result.ToApiResult();
    }
}
