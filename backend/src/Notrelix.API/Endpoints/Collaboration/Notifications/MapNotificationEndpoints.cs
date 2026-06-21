using Notrelix.API.Endpoints.Collaboration.Notifications.Commands;
using Notrelix.API.Endpoints.Collaboration.Notifications.Queries;

namespace Notrelix.API.Endpoints.Collaboration.Notifications;

public static class MapNotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/notifications")
            .WithTags("Collaboration.Notifications")
            .WithOpenApi()
            .RequireAuthorization();

        group.MapListNotifications();
        group.MapMarkAsRead();
        group.MapMarkAllAsRead();

        return app;
    }
}
