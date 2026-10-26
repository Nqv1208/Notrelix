using Notrelix.API.Extensions;
using Notrelix.Application.Features.Documents.Pages.Commands.RestorePage;

namespace Notrelix.API.Endpoints.Documents.Pages.Commands;

public static class RestorePageEndpoint
{
    public static IEndpointRouteBuilder MapRestorePage(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/restore", HandleAsync)
            .WithName("Documents.Pages.RestorePage")
            .WithTags("Documents.Pages")
            .WithSummary("Restore a page from the trash");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid pageId, ISender sender)
    {
        var result = await sender.Send(new RestorePageCommand(pageId));
        return result.ToNoContentResult();
    }
}
