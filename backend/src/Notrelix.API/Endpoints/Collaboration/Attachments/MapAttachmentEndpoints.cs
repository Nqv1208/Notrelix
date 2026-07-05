using Notrelix.API.Endpoints.Collaboration.Attachments.Commands;
using Notrelix.API.Endpoints.Collaboration.Attachments.Queries;

namespace Notrelix.API.Endpoints.Collaboration.Attachments;

public static class MapAttachmentEndpoints
{
    public static IEndpointRouteBuilder MapAttachmentsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/board-items/{boardItemId:guid}/attachments")
            .WithTags("Collaboration.Attachments")
            .WithOpenApi();

        group.MapGetAttachments();
        group.MapCreateAttachment();
        group.MapDeleteAttachment();

        return app;
    }
}
