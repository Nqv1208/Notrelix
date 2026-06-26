using Notrelix.API.Contracts.Documents.Pages.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Documents.Pages.Commands.CreatePage;

namespace Notrelix.API.Endpoints.Documents.Pages.Commands;

public static class CreatePageEndpoint
{
    public static IEndpointRouteBuilder MapCreatePage(this IEndpointRouteBuilder group)
    {
        group.MapPost("/", HandleAsync)
            .WithName("Documents.Pages.CreatePage")
            .WithTags("Documents.Pages")
            .WithSummary("Create a new page");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid workspaceId, CreatePageRequest body, ISender sender)
    {
        var result = await sender.Send(new CreatePageCommand(workspaceId, body.Title, body.ParentId));
        return result.ToCreatedResult();
    }
}

