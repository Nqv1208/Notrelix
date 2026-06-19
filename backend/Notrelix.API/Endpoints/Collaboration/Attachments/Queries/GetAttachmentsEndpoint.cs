using MediatR;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Collaboration.Attachments.Queries.GetCardAttachments;

namespace Notrelix.API.Endpoints.Collaboration.Attachments.Queries;

public static class GetAttachmentsEndpoint
{
    public static IEndpointRouteBuilder MapGetAttachments(this IEndpointRouteBuilder group)
    {
        group.MapGet("/", HandleAsync)
            .WithName("Collaboration.Attachments.Get")
            .WithTags("Collaboration.Attachments")
            .WithSummary("Get card attachments");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid cardId, ISender sender)
    {
        var result = await sender.Send(new GetCardAttachmentsQuery(cardId));
        return result.ToApiResult();
    }
}
