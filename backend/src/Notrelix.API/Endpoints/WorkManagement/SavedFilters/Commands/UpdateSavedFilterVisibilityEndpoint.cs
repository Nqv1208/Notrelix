using Notrelix.API.Contracts.WorkManagement.SavedFilters.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Views.Commands.UpdateSavedFilterVisibility;

namespace Notrelix.API.Endpoints.WorkManagement.SavedFilters.Commands;

public static class UpdateSavedFilterVisibilityEndpoint
{
    public static IEndpointRouteBuilder MapUpdateSavedFilterVisibility(this IEndpointRouteBuilder group)
    {
        group.MapResourcePatch("/visibility", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.SavedFilters.UpdateVisibility")
            .WithTags("WorkManagement.SavedFilters")
            .WithSummary("Update saved filter visibility");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid filterId,
        UpdateSavedFilterVisibilityRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateSavedFilterVisibilityCommand(filterId, body.Visibility, body.ExpectedVersion), cancellationToken);
        return result.ToNoContentResult();
    }
}
