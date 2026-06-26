using Notrelix.API.Extensions;
using Notrelix.Application.Features.Documents.Pages.Commands.DeletePage;

namespace Notrelix.API.Endpoints.Documents.Pages.Commands;

public static class DeletePageEndpoint
{
    public static IEndpointRouteBuilder MapDeletePage(this IEndpointRouteBuilder group)
    {
        group.MapDelete("/", HandleAsync)
            .WithName("Documents.Pages.DeletePage")
            .WithTags("Documents.Pages")
            .WithSummary("Delete a page");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid pageId, ISender sender)
    {
        var result = await sender.Send(new DeletePageCommand(pageId));
        return result.ToNoContentResult();
    }
}
