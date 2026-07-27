using Notrelix.API.Contracts.WorkManagement.BoardPreferences.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardPreferences.Commands.UpdateBoardPreferenceFilters;

namespace Notrelix.API.Endpoints.WorkManagement.BoardPreferences.Commands;

public static class UpdateBoardPreferenceFiltersEndpoint
{
    public static IEndpointRouteBuilder MapUpdateBoardPreferenceFilters(this IEndpointRouteBuilder group)
    {
        group.MapResourcePatch("/filters", HandleAsync)
            .WithName("WorkManagement.BoardPreferences.UpdateFilters")
            .WithTags("WorkManagement.BoardPreferences")
            .WithSummary("Update board preference filters for current user");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        Guid viewId,
        UpdateBoardPreferenceFiltersRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateBoardPreferenceFiltersCommand(boardId, viewId, body.Filters),
            cancellationToken);
        return result.ToNoContentResult();
    }
}
