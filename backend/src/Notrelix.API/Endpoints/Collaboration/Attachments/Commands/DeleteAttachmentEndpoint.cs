using Notrelix.API.Extensions;
using Notrelix.Application.Features.Collaboration.Attachments.Commands.DeleteAttachment;

namespace Notrelix.API.Endpoints.Collaboration.Attachments.Commands;

public static class DeleteAttachmentEndpoint
{
    public static IEndpointRouteBuilder MapDeleteAttachment(this IEndpointRouteBuilder group)
    {
        group.MapDelete("/{attachmentId:guid}", HandleAsync)
            .WithName("Collaboration.Attachments.Delete")
            .WithTags("Collaboration.Attachments")
            .WithSummary("Delete an attachment");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid attachmentId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteAttachmentCommand(attachmentId), cancellationToken);
        return result.ToNoContentResult();
    }
}
