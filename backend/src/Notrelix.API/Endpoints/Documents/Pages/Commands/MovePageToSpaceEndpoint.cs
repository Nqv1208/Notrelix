using Notrelix.API.Contracts.Documents.Pages.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Documents.Pages.Commands.MovePageToSpace;

namespace Notrelix.API.Endpoints.Documents.Pages.Commands;

public static class MovePageToSpaceEndpoint
{
    public static IEndpointRouteBuilder MapMovePageToSpace(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/move-to-space", HandleAsync)
            .WithName("Documents.Pages.MoveToSpace")
            .WithTags("Documents.Pages")
            .WithSummary("Move a root page and its subtree into a space or back to the workspace root");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid pageId, MovePageToSpaceRequest body, ISender sender)
    {
        var result = await sender.Send(new MovePageToSpaceCommand(pageId, body.SpaceId, body.ExpectedVersion));
        return result.ToNoContentResult();
    }
}
