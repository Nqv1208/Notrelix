using Notrelix.API.Extensions;
using Notrelix.Application.Features.Integrations.Calendar.Commands.ConnectCalendar;
using Notrelix.Application.Features.Integrations.Calendar.Commands.DisconnectCalendar;
using Notrelix.API.Contracts.Integrations.Calendar.Requests;
using Notrelix.Application.Features.Integrations.Calendar.Commands.HandleCalendarWebhook;

namespace Notrelix.API.Endpoints.Integrations.Calendar;

public static class CalendarEndpoints
{
    public static IEndpointRouteBuilder AddCalendarEndpoints(this IEndpointRouteBuilder app)
    {
        var workspaceGroup = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/calendar")
            .WithTags("Integrations.Calendar")
            .WithOpenApi();

        workspaceGroup.MapResourcePost("/connections", ConnectAsync)
            .WithName("Integrations.Calendar.Connect")
            .WithSummary("Connect a calendar provider to the workspace");

        var calendarGroup = app
            .MapGroup("/api/v1/calendar-integrations/{integrationId:guid}")
            .WithTags("Integrations.Calendar")
            .WithOpenApi();

        calendarGroup.MapResourceDelete("/", DisconnectAsync)
            .WithName("Integrations.Calendar.Disconnect")
            .WithSummary("Disconnect a calendar integration");

        var webhookGroup = app
            .MapGroup("/api/v1/integrations/calendar/webhooks/{provider}")
            .WithTags("Integrations.Calendar")
            .WithOpenApi();

        webhookGroup.MapPublicPost("/", WebhookAsync)
            .WithName("Integrations.Calendar.HandleWebhook")
            .WithSummary("Verified provider webhook callback intake")
            .AllowAnonymous();

        return app;
    }

    private static async Task<IResult> WebhookAsync(string provider, HttpRequest request, HandleCalendarWebhookCommandHandler handler, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(request.Body);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);

        var signature = request.Headers["X-Calendar-Signature"].FirstOrDefault() ?? string.Empty;
        var timestamp = request.Headers["X-Calendar-Timestamp"].FirstOrDefault() ?? string.Empty;

        var result = await handler.Handle(
            new HandleCalendarWebhookCommand(provider, signature, timestamp, rawBody),
            cancellationToken);

        return result.Succeeded ? Results.Ok() : Results.Unauthorized();
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
