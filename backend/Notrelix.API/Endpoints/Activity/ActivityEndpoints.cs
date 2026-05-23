using MediatR;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Shared.Activity;

namespace Notrelix.API.Endpoints.Activity;

public static class ActivityEndpoints
{
    public static IEndpointRouteBuilder MapActivityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/cards/{cardId:guid}/activity")
            .WithTags("Activity")
            .RequireAuthorization()
            .WithOpenApi();

        group.MapGet("/", GetCardActivity)
            .WithName("GetCardActivity")
            .WithSummary("Get activity log for a card");

        return app;
    }

    private static async Task<IResult> GetCardActivity(Guid cardId, ISender sender, int page = 1, int pageSize = 20)
    {
        var result = await sender.Send(new GetResourceActivityQuery("Card", cardId, page, pageSize));
        return result.ToApiResult();
    }
}
