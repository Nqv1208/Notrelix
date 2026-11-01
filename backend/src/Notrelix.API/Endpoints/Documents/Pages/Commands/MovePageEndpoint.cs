using Notrelix.API.Contracts.Documents.Pages.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Documents.Pages.Commands.MovePage;

namespace Notrelix.API.Endpoints.Documents.Pages.Commands;

public static class MovePageEndpoint
{
    public static IEndpointRouteBuilder MapMovePage(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/move", HandleAsync)
            .WithName("Documents.Pages.Move")
            .WithTags("Documents.Pages")
            .WithSummary("Move a page and its subtree under another parent page, or make it a root page of its space");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid pageId, MovePageRequest body, ISender sender)
    {
        var result = await sender.Send(new MovePageCommand(pageId, body.ParentId, body.ExpectedVersion));
        return result.ToNoContentResult();
    }
}
