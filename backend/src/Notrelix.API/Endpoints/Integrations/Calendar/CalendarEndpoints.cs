using Notrelix.API.Extensions;
using Notrelix.Application.Features.Integrations.Calendar.Commands.ConnectCalendar;
using Notrelix.Application.Features.Integrations.Calendar.Commands.DisconnectCalendar;

namespace Notrelix.API.Endpoints.Integrations.Calendar;

public static class CalendarEndpoints
{
    public static IEndpointRouteBuilder AddCalendarEndpoints(this IEndpointRouteBuilder app)
    {
        var workspaceGroup = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/calendar")
            .WithTags("Integrations.Calendar")
            .WithOpenApi();

        workspaceGroup.MapPost("/connections", ConnectAsync)
            .WithName("Integrations.Calendar.Connect")
            .WithSummary("Connect a calendar provider to the workspace");

        var calendarGroup = app
            .MapGroup("/api/v1/calendar-integrations/{integrationId:guid}")
            .WithTags("Integrations.Calendar")
            .WithOpenApi();

        calendarGroup.MapDelete("/", DisconnectAsync)
            .WithName("Integrations.Calendar.Disconnect")
            .WithSummary("Disconnect a calendar integration");

        return app;
    }

    private static async Task<IResult> ConnectAsync(Guid workspaceId, ConnectCalendarRequest body, ISender sender)
    {
        var result = await sender.Send(new ConnectCalendarCommand(
            body.Provider, body.AccessToken, workspaceId, body.ProviderAccountId, body.SyncDirection));
        return result.ToCreatedResult();
    }

    private static async Task<IResult> DisconnectAsync(Guid integrationId, ISender sender)
    {
        var result = await sender.Send(new DisconnectCalendarCommand(integrationId));
        return result.ToNoContentResult();
    }
}

public record ConnectCalendarRequest(string Provider, string AccessToken, Guid? ProviderAccountId, string SyncDirection);
