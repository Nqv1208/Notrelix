using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Labels.Queries.GetLabels;

namespace Notrelix.API.Endpoints.WorkManagement.Labels.Queries;

public static class ListLabelsEndpoint
{
    public static IEndpointRouteBuilder MapListLabels(this IEndpointRouteBuilder group)
    {
        group.MapGet("/", HandleAsync)
            .WithName("WorkManagement.Labels.List")
            .WithTags("WorkManagement.Labels")
            .WithSummary("Get all labels for a board");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetLabelsQuery(boardId), cancellationToken);
        return result.ToApiResult();
    }
}
