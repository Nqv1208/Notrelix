using Notrelix.API.Extensions;
using Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Application.Features.Documents.Pages.Queries.GetPageBacklinks;

namespace Notrelix.API.Endpoints.Documents.Pages.Queries;

public static class GetPageBacklinksEndpoint
{
    public static IEndpointRouteBuilder MapGetPageBacklinks(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/backlinks", HandleAsync)
            .WithName("Documents.Pages.GetPageBacklinks")
            .Produces<PageBacklinksDto>(StatusCodes.Status200OK, "application/json")
            .WithTags("Documents.Pages")
            .WithSummary("List the pages that mention a page");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid pageId, ISender sender)
    {
        var result = await sender.Send(new GetPageBacklinksQuery(pageId));
        return result.ToApiResult();
    }
}
