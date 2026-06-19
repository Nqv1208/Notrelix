using Notrelix.API.Endpoints.Collaboration.Attachments.Commands;
using Notrelix.API.Endpoints.Collaboration.Attachments.Queries;

namespace Notrelix.API.Endpoints.Collaboration.Attachments;

public static class MapAttachmentEndpoints
{
    public static IEndpointRouteBuilder MapAttachmentsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/cards/{cardId:guid}/attachments")
            .WithTags("Collaboration.Attachments")
            .RequireAuthorization()
            .WithOpenApi();

        group.MapGetAttachments();
        group.MapCreateAttachment();

        return app;
    }
}
