using Notrelix.API.Contracts.WorkManagement.SavedFilters.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Views.Commands.UpdateSavedFilterGroup;

namespace Notrelix.API.Endpoints.WorkManagement.SavedFilters.Commands;

public static class UpdateSavedFilterGroupEndpoint
{
    public static IEndpointRouteBuilder MapUpdateSavedFilterGroup(this IEndpointRouteBuilder group)
    {
        group.MapResourcePatch("/group", HandleAsync)
            .WithName("WorkManagement.SavedFilters.UpdateGroup")
            .WithTags("WorkManagement.SavedFilters")
            .WithSummary("Update saved filter group rule");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid filterId,
        UpdateSavedFilterGroupRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateSavedFilterGroupCommand(filterId, body.GroupRule, body.ExpectedVersion), cancellationToken);
        return result.ToNoContentResult();
    }
}
