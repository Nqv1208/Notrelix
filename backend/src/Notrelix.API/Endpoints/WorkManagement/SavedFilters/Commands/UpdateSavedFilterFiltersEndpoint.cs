using Notrelix.API.Contracts.WorkManagement.SavedFilters.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Views.Commands.UpdateSavedFilterFilters;

namespace Notrelix.API.Endpoints.WorkManagement.SavedFilters.Commands;

public static class UpdateSavedFilterFiltersEndpoint
{
    public static IEndpointRouteBuilder MapUpdateSavedFilterFilters(this IEndpointRouteBuilder group)
    {
        group.MapResourcePatch("/filters", HandleAsync)
            .WithName("WorkManagement.SavedFilters.UpdateFilters")
            .WithTags("WorkManagement.SavedFilters")
            .WithSummary("Update saved filter filter rules");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid filterId,
        UpdateSavedFilterFiltersRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateSavedFilterFiltersCommand(filterId, body.Rules, body.ExpectedVersion), cancellationToken);
        return result.ToNoContentResult();
    }
}
