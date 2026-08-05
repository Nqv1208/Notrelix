using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Templates.Queries.ListBoardTemplates;

namespace Notrelix.API.Endpoints.WorkManagement.Templates.Queries;

public static class ListBoardTemplatesEndpoint
{
    public static IEndpointRouteBuilder MapListBoardTemplates(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/", HandleAsync)
            .WithName("WorkManagement.Templates.List")
            .WithTags("WorkManagement.Templates")
            .WithSummary("List all board templates for a workspace");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListBoardTemplatesQuery(workspaceId), cancellationToken);
        return result.ToApiResult();
    }
}
