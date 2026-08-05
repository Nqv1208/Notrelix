using Notrelix.API.Contracts.WorkManagement.BoardPreferences.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardPreferences.Commands.CreateOrUpdateBoardPreference;

namespace Notrelix.API.Endpoints.WorkManagement.BoardPreferences.Commands;

public static class CreateOrUpdateBoardPreferenceEndpoint
{
    public static IEndpointRouteBuilder MapCreateOrUpdateBoardPreference(this IEndpointRouteBuilder group)
    {
        group.MapResourcePut("/", HandleAsync)
            .WithName("WorkManagement.BoardPreferences.CreateOrUpdate")
            .WithTags("WorkManagement.BoardPreferences")
            .WithSummary("Create or update board preference for current user");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        Guid viewId,
        CreateOrUpdateBoardPreferenceRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateOrUpdateBoardPreferenceCommand(boardId, viewId, body.Filters, body.Sorts, body.Group),
            cancellationToken);
        return result.ToNoContentResult();
    }
}
