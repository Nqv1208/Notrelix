using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Views.Queries.ListSavedFilters;

namespace Notrelix.API.Endpoints.WorkManagement.SavedFilters.Queries;

public static class ListSavedFiltersEndpoint
{
    public static IEndpointRouteBuilder MapListSavedFilters(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/", HandleAsync)
            .WithName("WorkManagement.SavedFilters.List")
            .WithTags("WorkManagement.SavedFilters")
            .WithSummary("List saved filters for a board");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListSavedFiltersQuery(boardId), cancellationToken);
        return result.ToApiResult();
    }
}
