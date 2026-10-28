using Notrelix.API.Extensions;
using Notrelix.Application.Features.Collaboration.Attachments.Queries.GetBoardItemAttachmentContent;

namespace Notrelix.API.Endpoints.Collaboration.Attachments.Queries;

public static class GetAttachmentContentEndpoint
{
    public static IEndpointRouteBuilder MapGetAttachmentContent(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/{attachmentId:guid}/content", HandleAsync)
            .WithName("Collaboration.Attachments.GetContent")
            .WithTags("Collaboration.Attachments")
            .WithSummary("Download the stored file of an uploaded attachment (authorised, streamed)")
            .Produces(StatusCodes.Status200OK, contentType: "application/octet-stream")
            .ProducesProblem(StatusCodes.Status404NotFound);
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardItemId,
        Guid attachmentId,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBoardItemAttachmentContentQuery(boardItemId, attachmentId), cancellationToken);
        if (!result.Succeeded)
        {
            return result.ToApiResult();
        }

        return AttachmentContentResponse.Create(httpContext, result.Data!);
    }
}
