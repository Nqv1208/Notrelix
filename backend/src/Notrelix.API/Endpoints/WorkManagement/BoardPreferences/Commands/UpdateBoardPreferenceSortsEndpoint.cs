using Notrelix.API.Contracts.WorkManagement.BoardPreferences.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardPreferences.Commands.UpdateBoardPreferenceSorts;

namespace Notrelix.API.Endpoints.WorkManagement.BoardPreferences.Commands;

public static class UpdateBoardPreferenceSortsEndpoint
{
    public static IEndpointRouteBuilder MapUpdateBoardPreferenceSorts(this IEndpointRouteBuilder group)
    {
        group.MapResourcePatch("/sorts", HandleAsync)
            .WithName("WorkManagement.BoardPreferences.UpdateSorts")
            .WithTags("WorkManagement.BoardPreferences")
            .WithSummary("Update board preference sorts for current user");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        Guid viewId,
        UpdateBoardPreferenceSortsRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateBoardPreferenceSortsCommand(boardId, viewId, body.Sorts),
            cancellationToken);
        return result.ToNoContentResult();
    }
}
