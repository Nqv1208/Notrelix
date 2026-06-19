using MediatR;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Documents.Pages.Queries.SearchPages;

namespace Notrelix.API.Endpoints.Documents.Pages.Queries;

public static class SearchPagesEndpoint
{
    public static IEndpointRouteBuilder MapSearchPages(this IEndpointRouteBuilder group)
    {
        group.MapGet("/search", HandleAsync)
            .WithName("Documents.Pages.SearchPages")
            .WithTags("Documents.Pages")
            .WithSummary("Search pages in a workspace");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid workspaceId, string query, ISender sender)
    {
        var result = await sender.Send(new SearchPagesQuery(workspaceId, query));
        return result.ToApiResult();
    }
}
