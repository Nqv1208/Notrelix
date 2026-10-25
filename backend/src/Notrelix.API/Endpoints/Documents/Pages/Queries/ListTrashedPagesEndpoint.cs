using Notrelix.API.Extensions;
using Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Application.Features.Documents.Pages.Queries.GetTrashedPages;

namespace Notrelix.API.Endpoints.Documents.Pages.Queries;

public static class ListTrashedPagesEndpoint
{
    public static IEndpointRouteBuilder MapListTrashedPages(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceGet("/trash", HandleAsync)
            .WithName("Documents.Pages.ListTrashedPages")
            .Produces<List<PageDto>>(StatusCodes.Status200OK, "application/json")
            .WithTags("Documents.Pages")
            .WithSummary("List the pages in the trash of a workspace");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid workspaceId, ISender sender)
    {
        var result = await sender.Send(new GetTrashedPagesQuery(workspaceId));
        return result.ToApiResult();
    }
}
