using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Views.Commands.RestoreSavedFilter;

namespace Notrelix.API.Endpoints.WorkManagement.SavedFilters.Commands;

public static class RestoreSavedFilterEndpoint
{
    public static IEndpointRouteBuilder MapRestoreSavedFilter(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/restore", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.SavedFilters.Restore")
            .WithTags("WorkManagement.SavedFilters")
            .WithSummary("Restore a soft-deleted saved filter");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid filterId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RestoreSavedFilterCommand(filterId), cancellationToken);
        return result.ToNoContentResult();
    }
}
