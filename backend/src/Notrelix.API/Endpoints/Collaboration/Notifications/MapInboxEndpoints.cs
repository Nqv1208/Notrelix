using Notrelix.API.Extensions;
using Notrelix.Application.Features.Collaboration.Notifications.DTOs;
using Notrelix.Application.Features.Collaboration.Notifications.Queries.GetInboxSummary;

namespace Notrelix.API.Endpoints.Collaboration.Notifications;

/// <summary>The caller's own notification inbox, owned by Collaboration.</summary>
public static class MapInboxEndpoints
{
    public static IEndpointRouteBuilder RegisterInboxEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/inbox")
            .WithTags("Collaboration.Notifications")
            .WithOpenApi();

        group.MapAuthenticatedGet("/summary", async (ISender sender) =>
                (await sender.Send(new GetInboxSummaryQuery())).ToApiResult())
            .WithName("Collaboration.Notifications.GetInboxSummary")
            .WithSummary("Get the caller's unread notification counts per workspace")
            .WithDescription("Unread notifications the caller holds in each workspace they are an active member of, and their total; workspaces with none are omitted. Only the caller's own notifications are counted.")
            .Produces<InboxSummaryDto>(StatusCodes.Status200OK, "application/json");

        return app;
    }
}
