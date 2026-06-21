using MediatR;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Collaboration.Notifications.Queries.GetUserNotifications;

namespace Notrelix.API.Endpoints.Collaboration.Notifications.Queries;

public static class ListNotificationsEndpoint
{
    public static IEndpointRouteBuilder MapListNotifications(this IEndpointRouteBuilder group)
    {
        group.MapGet("/", HandleAsync)
            .WithName("Collaboration.Notifications.List")
            .WithTags("Collaboration.Notifications")
            .WithSummary("Get notifications for the logged-in user");
        return group;
    }

    private static async Task<IResult> HandleAsync(ISender sender)
    {
        var result = await sender.Send(new GetUserNotificationsQuery());
        return result.ToApiResult();
    }
}
