using Notrelix.API.Extensions;
using Notrelix.Application.Features.Collaboration.Notifications.Commands.MarkAllNotificationsAsRead;

namespace Notrelix.API.Endpoints.Collaboration.Notifications.Commands;

public static class MarkAllNotificationsAsReadEndpoint
{
    public static IEndpointRouteBuilder MapMarkAllAsRead(this IEndpointRouteBuilder group)
    {
        group.MapPost("/read-all", HandleAsync)
            .WithName("Collaboration.Notifications.MarkAllAsRead")
            .WithTags("Collaboration.Notifications")
            .WithSummary("Mark all user notifications as read");
        return group;
    }

    private static async Task<IResult> HandleAsync(ISender sender)
    {
        var result = await sender.Send(new MarkAllNotificationsAsReadCommand());
        return result.ToApiResult();
    }
}
