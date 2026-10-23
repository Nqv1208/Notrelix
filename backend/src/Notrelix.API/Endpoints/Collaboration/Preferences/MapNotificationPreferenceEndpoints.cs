using Notrelix.API.Contracts.Collaboration.Preferences.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Collaboration.Preferences.Commands.UpdateNotificationPreferences;
using Notrelix.Application.Features.Collaboration.Preferences.DTOs;
using Notrelix.Application.Features.Collaboration.Preferences.Queries.GetNotificationPreferences;

namespace Notrelix.API.Endpoints.Collaboration.Preferences;

/// <summary>
/// The caller's own notification preference matrix per Workspace, owned by Collaboration: notification
/// type by channel (in-app, email). Only the caller's matrix is addressable.
/// </summary>
public static class MapNotificationPreferenceEndpoints
{
    public static IEndpointRouteBuilder RegisterNotificationPreferenceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/notification-preferences")
            .WithTags("Collaboration.Preferences")
            .WithOpenApi();

        group.MapWorkspaceGet("/", async (Guid workspaceId, ISender sender) =>
                (await sender.Send(new GetNotificationPreferencesQuery(workspaceId))).ToApiResult())
            .WithName("Collaboration.Preferences.GetNotificationPreferences")
            .WithSummary("Get the caller's notification preferences in a workspace")
            .WithDescription("One row per notification type (assigned, mention, approval, overdue, watching, invitation, digest) with its in-app and email value; a cell the caller never changed has its default. Version is 0 until the first change is saved.")
            .Produces<NotificationPreferencesDto>(StatusCodes.Status200OK, "application/json");

        group.MapWorkspacePatch("/", async (Guid workspaceId, UpdateNotificationPreferencesRequest body, ISender sender) =>
            {
                if (body.ExpectedVersion is not { } expectedVersion)
                    return EndpointExtensions.InvalidInput("expectedVersion is required.");
                if (body.Rows is null)
                    return EndpointExtensions.InvalidInput("rows is required.");

                var rows = body.Rows
                    .Select(row => new NotificationPreferenceRowChange(row.Type, row.InApp, row.Email))
                    .ToList();
                return (await sender.Send(new UpdateNotificationPreferencesCommand(workspaceId, expectedVersion, rows))).ToApiResult();
            })
            .WithName("Collaboration.Preferences.UpdateNotificationPreferences")
            .WithSummary("Change the caller's notification preferences in a workspace")
            .WithDescription("Partial and all-or-nothing: only the named channels of the named types change. Answers the full matrix, 412 when expectedVersion is stale, 400 or 422 for an unknown type or no change requested. Setting values that already hold changes nothing and keeps the version.")
            .Produces<NotificationPreferencesDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status412PreconditionFailed);

        return app;
    }
}
