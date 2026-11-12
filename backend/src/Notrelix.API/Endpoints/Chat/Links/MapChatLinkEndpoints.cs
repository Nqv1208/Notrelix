using Notrelix.API.Extensions;
using Notrelix.Application.Features.Chat.Links.DTOs;
using Notrelix.Application.Features.Chat.Links.Queries.ResolveLink;

namespace Notrelix.API.Endpoints.Chat.Links;

/// <summary>
/// Link previews for a Board item or a Page in a message (UC-CHT-G05, CHT-023). A preview is resolved on demand with the reader's own access
/// and is never stored on the message; resolving never changes the resource.
/// </summary>
public static class MapChatLinkEndpoints
{
    public static IEndpointRouteBuilder MapChatLinkRoutes(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/chat")
            .WithTags("Chat.Links")
            .WithOpenApi();

        group.MapWorkspaceGet("/links/resolve", ResolveAsync)
            .WithName("Chat.Links.Resolve")
            .WithSummary("Resolve the preview of a link to a Board item or a Page")
            .WithDescription("Send either url (a product URL of this Workspace, at most 2048 characters: /workspaces/{id}/boards/{id}?item={id} or /workspaces/{id}/docs/{id}) or kind (work-management.board-item or documents.page) with resourceId. The title is returned only when the caller can view the resource now; otherwise accessible is false and there is no title, the same answer whether the resource is hidden, trashed or missing. A URL that is not a Board item or Page of this Workspace is 400. Nothing is changed or granted.")
            .Produces<LinkPreviewDto>(StatusCodes.Status200OK)
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden);

        return app;
    }

    private static async Task<IResult> ResolveAsync(Guid workspaceId, string? url, string? kind, Guid? resourceId, ISender sender)
    {
        var result = await sender.Send(new ResolveLinkQuery(workspaceId, url, kind, resourceId));
        return result.ToApiResult();
    }
}
