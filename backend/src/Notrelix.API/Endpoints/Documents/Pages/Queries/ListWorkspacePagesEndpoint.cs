using Notrelix.API.Extensions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Application.Features.Documents.Pages.Queries.GetWorkspacePages;

namespace Notrelix.API.Endpoints.Documents.Pages.Queries;

public static class ListWorkspacePagesEndpoint
{
    public static IEndpointRouteBuilder MapListWorkspacePages(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceGet("/", HandleAsync)
            .WithName("Documents.Pages.ListWorkspacePages")
            .Produces<CursorPage<PageDto>>(StatusCodes.Status200OK, "application/json")
            .WithTags("Documents.Pages")
            .WithSummary("List the pages in a workspace, one cursor page at a time")
            .WithDescription("Id order, default 50 and at most 200 a page. Pass nextCursor back as cursor for the next page.");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid workspaceId, string? cursor, int? limit, ISender sender)
    {
        var result = await sender.Send(new GetWorkspacePagesQuery(workspaceId, cursor, limit));
        return result.ToApiResult();
    }
}
