using Notrelix.API.Contracts.Collaboration.Attachments.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Collaboration.Attachments.Commands.CreateBoardItemAttachment;

namespace Notrelix.API.Endpoints.Collaboration.Attachments.Commands;

public static class CreateAttachmentEndpoint
{
    public static IEndpointRouteBuilder MapCreateAttachment(this IEndpointRouteBuilder group)
    {
        group.MapPost("/", HandleAsync)
            .WithName("Collaboration.Attachments.Create")
            .WithTags("Collaboration.Attachments")
            .WithSummary("Register card attachment metadata");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid cardId, CreateBoardItemAttachmentRequest body, ISender sender)
    {
        var result = await sender.Send(new CreateBoardItemAttachmentCommand(cardId, body.Filename, body.Url, body.SizeBytes, body.ContentType, body.Source));
        return result.ToCreatedResult();
    }
}

