using MediatR;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Shared.Queries.Notifications;
using Notrelix.Application.Features.Shared.Commands.Notifications;

namespace Notrelix.API.Endpoints.Notifications;

public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/notifications")
            .WithTags("Notifications")
            .WithOpenApi()
            .RequireAuthorization();

        group.MapGet("/", GetNotifications)
            .WithName("GetUserNotifications")
            .WithSummary("Get notifications for the logged-in user");

        group.MapPost("/{id:guid}/read", MarkAsRead)
            .WithName("MarkNotificationAsRead")
            .WithSummary("Mark a specific notification as read");

        group.MapPost("/read-all", MarkAllAsRead)
            .WithName("MarkAllNotificationsAsRead")
            .WithSummary("Mark all user notifications as read");

        return app;
    }

    private static async Task<IResult> GetNotifications(
        ISender sender)
    {
        var result = await sender.Send(new GetUserNotificationsQuery());
        return result.ToApiResult();
    }

    private static async Task<IResult> MarkAsRead(
        Guid id,
        ISender sender)
    {
        var result = await sender.Send(new MarkNotificationAsReadCommand(id));
        return result.ToApiResult();
    }

    private static async Task<IResult> MarkAllAsRead(
        ISender sender)
    {
        var result = await sender.Send(new MarkAllNotificationsAsReadCommand());
        return result.ToApiResult();
    }
}
