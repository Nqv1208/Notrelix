using Notrelix.API.Contracts.WorkManagement.SavedFilters.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Views.Commands.UpdateSavedFilterSorts;

namespace Notrelix.API.Endpoints.WorkManagement.SavedFilters.Commands;

public static class UpdateSavedFilterSortsEndpoint
{
    public static IEndpointRouteBuilder MapUpdateSavedFilterSorts(this IEndpointRouteBuilder group)
    {
        group.MapResourcePatch("/sorts", HandleAsync)
            .WithName("WorkManagement.SavedFilters.UpdateSorts")
            .WithTags("WorkManagement.SavedFilters")
            .WithSummary("Update saved filter sort rules");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid filterId,
        UpdateSavedFilterSortsRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateSavedFilterSortsCommand(filterId, body.SortRules, body.ExpectedVersion), cancellationToken);
        return result.ToNoContentResult();
    }
}
