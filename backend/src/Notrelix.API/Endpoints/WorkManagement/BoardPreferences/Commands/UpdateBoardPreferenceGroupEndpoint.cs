using Notrelix.API.Contracts.WorkManagement.BoardPreferences.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardPreferences.Commands.UpdateBoardPreferenceGroup;

namespace Notrelix.API.Endpoints.WorkManagement.BoardPreferences.Commands;

public static class UpdateBoardPreferenceGroupEndpoint
{
    public static IEndpointRouteBuilder MapUpdateBoardPreferenceGroup(this IEndpointRouteBuilder group)
    {
        group.MapResourcePatch("/group", HandleAsync)
            .WithName("WorkManagement.BoardPreferences.UpdateGroup")
            .WithTags("WorkManagement.BoardPreferences")
            .WithSummary("Update board preference group for current user");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        Guid viewId,
        UpdateBoardPreferenceGroupRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateBoardPreferenceGroupCommand(boardId, viewId, body.Group),
            cancellationToken);
        return result.ToNoContentResult();
    }
}
