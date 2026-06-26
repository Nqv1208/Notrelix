using Notrelix.API.Extensions;
using Notrelix.Application.Features.Documents.Pages.Queries.GetWorkspacePages;

namespace Notrelix.API.Endpoints.Documents.Pages.Queries;

public static class ListWorkspacePagesEndpoint
{
    public static IEndpointRouteBuilder MapListWorkspacePages(this IEndpointRouteBuilder group)
    {
        group.MapGet("/", HandleAsync)
            .WithName("Documents.Pages.ListWorkspacePages")
            .WithTags("Documents.Pages")
            .WithSummary("List all pages in a workspace");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid workspaceId, ISender sender)
    {
        var result = await sender.Send(new GetWorkspacePagesQuery(workspaceId));
        return result.ToApiResult();
    }
}
