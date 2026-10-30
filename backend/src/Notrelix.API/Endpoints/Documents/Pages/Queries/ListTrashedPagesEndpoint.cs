using Notrelix.API.Extensions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Application.Features.Documents.Pages.Queries.GetTrashedPages;

namespace Notrelix.API.Endpoints.Documents.Pages.Queries;

public static class ListTrashedPagesEndpoint
{
    public static IEndpointRouteBuilder MapListTrashedPages(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceGet("/trash", HandleAsync)
            .WithName("Documents.Pages.ListTrashedPages")
            .Produces<CursorPage<PageDto>>(StatusCodes.Status200OK, "application/json")
            .WithTags("Documents.Pages")
            .WithSummary("List the pages in the trash of a workspace, newest change first, one cursor page at a time");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid workspaceId, string? cursor, int? limit, ISender sender)
    {
        var result = await sender.Send(new GetTrashedPagesQuery(workspaceId, cursor, limit));
        return result.ToApiResult();
    }
}
