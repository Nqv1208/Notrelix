using Notrelix.API.Contracts.Collaboration.Notifications.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Collaboration.Notifications.Commands.MarkInboxEntryRead;
using Notrelix.Application.Features.Collaboration.Notifications.Commands.MarkInboxEntryDone;
using Notrelix.Application.Features.Collaboration.Notifications.Commands.MarkWorkspaceInboxRead;
using Notrelix.Application.Features.Collaboration.Notifications.Commands.MuteInboxThread;
using Notrelix.Application.Features.Collaboration.Notifications.Commands.ReopenInboxEntry;
using Notrelix.Application.Features.Collaboration.Notifications.Commands.SnoozeInboxEntry;
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

        workspaceInbox.MapWorkspaceGet("/", async (Guid workspaceId, bool? unreadOnly, string? cursor, int? limit, string? state, ISender sender) =>
                (await sender.Send(new GetWorkspaceInboxQuery(workspaceId, unreadOnly ?? false, cursor, limit, state))).ToApiResult())
            .WithName("Collaboration.Notifications.GetWorkspaceInbox")
            .WithSummary("List the caller's own notifications in a workspace")
            .WithDescription("Newest first, one page at a time (limit 1 to 50, default 20; pass nextCursor to continue). Only the caller's own notifications; one about an Item on a Board the caller can no longer read is not shown. Archived and dismissed notifications are never listed. state selects the list: inbox (default: not done, not snoozed), done, or snoozed; a snoozed entry whose time has passed is back in inbox, unread. Each entry says whether it still needs the caller's reply (a mention they have not answered or finished), whether its thread is muted and, when snoozed, until when.")
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

        workspaceInbox.MapWorkspacePost("/{notificationId:guid}/done", async (Guid workspaceId, Guid notificationId, ISender sender) =>
                (await sender.Send(new MarkInboxEntryDoneCommand(workspaceId, notificationId))).ToApiResult())
            .WithName("Collaboration.Notifications.MarkInboxEntryDone")
            .WithSummary("Mark one notification of the caller done")
            .WithDescription("A done notification is read, leaves the default list (it shows under state=done) and no longer needs a reply. A notification that is not the caller's, or already done, changes nothing and still answers success.");

        workspaceInbox.MapWorkspacePost("/{notificationId:guid}/reopen", async (Guid workspaceId, Guid notificationId, ISender sender) =>
                (await sender.Send(new ReopenInboxEntryCommand(workspaceId, notificationId))).ToApiResult())
            .WithName("Collaboration.Notifications.ReopenInboxEntry")
            .WithSummary("Take one done or snoozed notification back into the inbox")
            .WithDescription("Clears done and snooze. A notification that is not the caller's, or already in the inbox, changes nothing and still answers success.");

        workspaceInbox.MapWorkspacePost("/{notificationId:guid}/snooze", async (Guid workspaceId, Guid notificationId, SnoozeInboxEntryRequest body, ISender sender) =>
                (await sender.Send(new SnoozeInboxEntryCommand(workspaceId, notificationId, body.Until ?? default))).ToApiResult())
            .WithName("Collaboration.Notifications.SnoozeInboxEntry")
            .WithSummary("Hide one notification of the caller until a later time")
            .WithDescription("The entry leaves the default list and returns, unread, once the time has passed. The time must be in the future and within a year.");

        workspaceInbox.MapWorkspacePost("/{notificationId:guid}/mute", async (Guid workspaceId, Guid notificationId, ISender sender) =>
                (await sender.Send(new MuteInboxThreadCommand(workspaceId, notificationId, true))).ToApiResult())
            .WithName("Collaboration.Notifications.MuteInboxThread")
            .WithSummary("Mute the thread a notification is about")
            .WithDescription("No new notification from that item or page reaches the caller; earlier ones stay. A repeat changes nothing.");

        workspaceInbox.MapWorkspacePost("/{notificationId:guid}/unmute", async (Guid workspaceId, Guid notificationId, ISender sender) =>
                (await sender.Send(new MuteInboxThreadCommand(workspaceId, notificationId, false))).ToApiResult())
            .WithName("Collaboration.Notifications.UnmuteInboxThread")
            .WithSummary("Lift the mute of the thread a notification is about")
            .WithDescription("A thread that is not muted changes nothing and still answers success.");

        return app;
    }
}
