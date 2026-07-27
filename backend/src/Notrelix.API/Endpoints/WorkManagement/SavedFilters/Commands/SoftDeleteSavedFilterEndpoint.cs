using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Views.Commands.SoftDeleteSavedFilter;

namespace Notrelix.API.Endpoints.WorkManagement.SavedFilters.Commands;

public static class SoftDeleteSavedFilterEndpoint
{
    public static IEndpointRouteBuilder MapSoftDeleteSavedFilter(this IEndpointRouteBuilder group)
    {
        group.MapResourceDelete("/", HandleAsync)
            .WithName("WorkManagement.SavedFilters.SoftDelete")
            .WithTags("WorkManagement.SavedFilters")
            .WithSummary("Soft delete a saved filter");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid filterId,
        [FromQuery] long expectedVersion,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SoftDeleteSavedFilterCommand(filterId, expectedVersion), cancellationToken);
        return result.ToNoContentResult();
    }
}
