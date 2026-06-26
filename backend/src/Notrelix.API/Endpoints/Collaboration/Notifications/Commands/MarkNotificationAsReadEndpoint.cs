using Notrelix.API.Extensions;
using Notrelix.Application.Features.Collaboration.Notifications.Commands.MarkNotificationAsRead;

namespace Notrelix.API.Endpoints.Collaboration.Notifications.Commands;

public static class MarkNotificationAsReadEndpoint
{
    public static IEndpointRouteBuilder MapMarkAsRead(this IEndpointRouteBuilder group)
    {
        group.MapPost("/{id:guid}/read", HandleAsync)
            .WithName("Collaboration.Notifications.MarkAsRead")
            .WithTags("Collaboration.Notifications")
            .WithSummary("Mark a specific notification as read");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid id, ISender sender)
    {
        var result = await sender.Send(new MarkNotificationAsReadCommand(id));
        return result.ToApiResult();
    }
}
