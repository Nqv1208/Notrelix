using Notrelix.API.Extensions;
using Notrelix.Application.Features.Documents.Pages.Commands.ArchivePage;

namespace Notrelix.API.Endpoints.Documents.Pages.Commands;

public static class ArchivePageEndpoint
{
    public static IEndpointRouteBuilder MapArchivePage(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/archive", HandleAsync)
            .WithName("Documents.Pages.ArchivePage")
            .WithTags("Documents.Pages")
            .WithSummary("Archive a page");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid pageId, ISender sender)
    {
        var result = await sender.Send(new ArchivePageCommand(pageId));
        return result.ToNoContentResult();
    }
}
