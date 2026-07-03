using Notrelix.API.Extensions;
using Notrelix.Application.Features.Collaboration.Attachments.Queries.GetBoardItemAttachments;

namespace Notrelix.API.Endpoints.Collaboration.Attachments.Queries;

public static class GetAttachmentsEndpoint
{
    public static IEndpointRouteBuilder MapGetAttachments(this IEndpointRouteBuilder group)
    {
        group.MapGet("/", HandleAsync)
            .WithName("Collaboration.Attachments.Get")
            .WithTags("Collaboration.Attachments")
            .WithSummary("Get board item attachments");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid boardItemId, ISender sender)
    {
        var result = await sender.Send(new GetBoardItemAttachmentsQuery(boardItemId));
        return result.ToApiResult();
    }
}
