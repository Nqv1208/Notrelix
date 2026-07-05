using Notrelix.API.Contracts.Documents.Pages.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Documents.Pages.Commands.UpdatePage;

namespace Notrelix.API.Endpoints.Documents.Pages.Commands;

public static class UpdatePageEndpoint
{
    public static IEndpointRouteBuilder MapUpdatePage(this IEndpointRouteBuilder group)
    {
        group.MapResourcePatch("/", HandleAsync)
            .WithName("Documents.Pages.UpdatePage")
            .WithTags("Documents.Pages")
            .WithSummary("Update a page");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid pageId, UpdatePageRequest body, ISender sender)
    {
        var result = await sender.Send(new UpdatePageCommand(pageId, body.Title));
        return result.ToApiResult();
    }
}

