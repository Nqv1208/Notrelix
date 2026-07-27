using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardPreferences.Queries.GetBoardPreference;

namespace Notrelix.API.Endpoints.WorkManagement.BoardPreferences.Queries;

public static class GetBoardPreferenceEndpoint
{
    public static IEndpointRouteBuilder MapGetBoardPreference(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/", HandleAsync)
            .WithName("WorkManagement.BoardPreferences.Get")
            .WithTags("WorkManagement.BoardPreferences")
            .WithSummary("Get board preference for current user");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        Guid viewId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetBoardPreferenceQuery(boardId, viewId),
            cancellationToken);
        return result.ToApiResult();
    }
}
