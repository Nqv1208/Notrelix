using Notrelix.API.Contracts.WorkManagement.SavedFilters.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Views.Commands.RenameSavedFilter;

namespace Notrelix.API.Endpoints.WorkManagement.SavedFilters.Commands;

public static class RenameSavedFilterEndpoint
{
    public static IEndpointRouteBuilder MapRenameSavedFilter(this IEndpointRouteBuilder group)
    {
        group.MapResourcePatch("/rename", HandleAsync)
            .WithName("WorkManagement.SavedFilters.Rename")
            .WithTags("WorkManagement.SavedFilters")
            .WithSummary("Rename a saved filter");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid filterId,
        RenameSavedFilterRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RenameSavedFilterCommand(filterId, body.Name, body.ExpectedVersion), cancellationToken);
        return result.ToNoContentResult();
    }
}
