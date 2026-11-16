using Notrelix.API.Contracts.Documents.Pages.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Application.Features.Documents.Pages.Commands.DuplicatePage;

namespace Notrelix.API.Endpoints.Documents.Pages.Commands;

public static class DuplicatePageEndpoint
{
    public static IEndpointRouteBuilder MapDuplicatePage(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePost("/{pageId:guid}/duplicate", HandleAsync)
            .WithIdempotencyKey()
            .WithName("Documents.Pages.DuplicatePage")
            .WithTags("Documents.Pages")
            .WithSummary("Duplicate a page without its subpages")
            .Produces<DuplicatedPageDto>(StatusCodes.Status201Created, "application/json");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid workspaceId, Guid pageId, DuplicatePageRequest body, ISender sender)
    {
        var result = await sender.Send(new DuplicatePageCommand(workspaceId, pageId, body.Title));
        return result.ToCreatedResult(result.Succeeded ? $"/api/v1/pages/{result.Data!.PageId}" : null);
    }
}
