using Notrelix.API.Extensions;
using Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Application.Features.Documents.Pages.Queries.GetPageTree;

namespace Notrelix.API.Endpoints.Documents.Pages.Queries;

public static class GetPageTreeEndpoint
{
    public static IEndpointRouteBuilder MapGetPageTree(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceGet("/tree", HandleAsync)
            .WithName("Documents.Pages.GetPageTree")
            .Produces<List<PageTreeItemDto>>(StatusCodes.Status200OK, "application/json")
            .WithTags("Documents.Pages")
            .WithSummary("Get the page tree for a workspace; a very large workspace answers one level at a time (root pages, or the children of parentId)");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid workspaceId, Guid? parentId, ISender sender)
    {
        var result = await sender.Send(new GetPageTreeQuery(workspaceId, parentId));
        return result.ToApiResult();
    }
}
