using Notrelix.API.Extensions;
using Notrelix.Application.Features.Collaboration.Activity.DTOs;
using Notrelix.Application.Features.Collaboration.Activity.Queries.GetPageActivity;

namespace Notrelix.API.Endpoints.Documents.Pages.Queries;

public static class GetPageActivityEndpoint
{
    public static IEndpointRouteBuilder MapGetPageActivity(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/activity", HandleAsync)
            .WithName("Documents.Pages.GetPageActivity")
            .Produces<ResourceActivityPageDto>(StatusCodes.Status200OK, "application/json")
            .WithTags("Documents.Pages")
            .WithSummary("Get the activity feed of a page (rename, move, trash, restore)");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid pageId, ISender sender, int page = 1, int pageSize = 20)
    {
        var result = await sender.Send(new GetPageActivityQuery(pageId, page, pageSize));
        return result.ToApiResult();
    }
}
