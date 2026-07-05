using Notrelix.API.Extensions;
using Notrelix.Application.Features.Documents.Pages.Queries.GetPageTree;

namespace Notrelix.API.Endpoints.Documents.Pages.Queries;

public static class GetPageTreeEndpoint
{
    public static IEndpointRouteBuilder MapGetPageTree(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceGet("/tree", HandleAsync)
            .WithName("Documents.Pages.GetPageTree")
            .WithTags("Documents.Pages")
            .WithSummary("Get page tree for a workspace");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid workspaceId, ISender sender)
    {
        var result = await sender.Send(new GetPageTreeQuery(workspaceId));
        return result.ToApiResult();
    }
}
