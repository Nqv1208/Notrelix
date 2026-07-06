using Notrelix.API.Extensions;
using Notrelix.Application.Features.Collaboration.Activity.Queries.GetResourceActivity;

namespace Notrelix.API.Endpoints.Collaboration.Activity.Queries;

public static class GetResourceActivityEndpoint
{
    public static IEndpointRouteBuilder MapGetBoardItemActivity(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/", HandleAsync)
            .WithName("Collaboration.Activity.GetBoardItemActivity")
            .WithTags("Collaboration.Activity")
            .WithSummary("Get activity log for a board item");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid boardItemId, ISender sender, int page = 1, int pageSize = 20)
    {
        var result = await sender.Send(new GetResourceActivityQuery(Enum.Parse<ResourceType>("BoardItem", ignoreCase: true), boardItemId, page, pageSize));
        return result.ToApiResult();
    }
}
