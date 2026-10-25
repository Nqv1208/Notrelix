using Notrelix.API.Extensions;
using Notrelix.Application.Features.Collaboration.Notifications.Commands.MarkInboxEntryRead;
using Notrelix.Application.Features.Collaboration.Notifications.Commands.MarkWorkspaceInboxRead;
using Notrelix.Application.Features.Collaboration.Notifications.DTOs;
using Notrelix.Application.Features.Collaboration.Notifications.Queries.GetInboxSummary;
using Notrelix.Application.Features.WorkManagement.BoardItems.Queries.GetWorkspaceInbox;

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

        var workspaceInbox = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/inbox")
            .WithTags("Collaboration.Notifications")
            .WithOpenApi();

        workspaceInbox.MapWorkspaceGet("/", async (Guid workspaceId, bool? unreadOnly, string? cursor, int? limit, ISender sender) =>
                (await sender.Send(new GetWorkspaceInboxQuery(workspaceId, unreadOnly ?? false, cursor, limit))).ToApiResult())
            .WithName("Collaboration.Notifications.GetWorkspaceInbox")
            .WithSummary("List the caller's own notifications in a workspace")
            .WithDescription("Newest first, one page at a time (limit 1 to 50, default 20; pass nextCursor to continue). Only the caller's own notifications; one about an Item on a Board the caller can no longer read is not shown. archived and dismissed notifications are not listed.")
            .Produces<WorkspaceInboxDto>(StatusCodes.Status200OK, "application/json");

        workspaceInbox.MapWorkspacePost("/read-all", async (Guid workspaceId, ISender sender) =>
                (await sender.Send(new MarkWorkspaceInboxReadCommand(workspaceId))).ToApiResult())
            .WithName("Collaboration.Notifications.MarkWorkspaceInboxRead")
            .WithSummary("Mark every notification of the caller in a workspace read")
            .WithDescription("Only the caller's own unread notifications change; answers how many.")
            .Produces<MarkedReadDto>(StatusCodes.Status200OK, "application/json");

        workspaceInbox.MapWorkspacePost("/{notificationId:guid}/read", async (Guid workspaceId, Guid notificationId, ISender sender) =>
                (await sender.Send(new MarkInboxEntryReadCommand(workspaceId, notificationId))).ToApiResult())
            .WithName("Collaboration.Notifications.MarkInboxEntryRead")
            .WithSummary("Mark one notification of the caller read")
            .WithDescription("A notification that is not the caller's, or already read, changes nothing and still answers success.");

        return app;
    }
}
