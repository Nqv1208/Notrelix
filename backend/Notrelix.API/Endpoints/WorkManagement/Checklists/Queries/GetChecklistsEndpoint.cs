using MediatR;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Checklists.Queries.GetChecklists;

namespace Notrelix.API.Endpoints.WorkManagement.Checklists.Queries;

public static class GetChecklistsEndpoint
{
    public static IEndpointRouteBuilder MapGetChecklists(this IEndpointRouteBuilder group)
    {
        group.MapGet("/", HandleAsync)
            .WithName("WorkManagement.Checklists.List")
            .WithTags("WorkManagement.Checklists")
            .WithSummary("Get checklists for a board item");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetChecklistsQuery(itemId), cancellationToken);
        return result.ToApiResult();
    }
}
