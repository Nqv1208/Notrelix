using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Limits;

namespace Notrelix.API.Endpoints.WorkManagement.Limits;

/// <summary>Publishes the board limits (D78.8, UC-WM-T03) so a client can say them before a write is refused.</summary>
public static class GetWorkLimitsEndpoint
{
    public static IEndpointRouteBuilder MapWorkLimits(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/work-management")
            .WithTags("WorkManagement.Limits")
            .WithOpenApi();

        group.MapAuthenticatedGet("/limits", HandleAsync)
            .WithName("WorkManagement.Limits.Get")
            .Produces<WorkLimitsDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblems(StatusCodes.Status401Unauthorized)
            .WithSummary("Get the published board limits")
            .WithDescription("Active items per board, fields, groups and views per board, and items per bulk edit. A write over one is refused with 409 and a work-management.limit.* error code carrying limit, current and max.");
        return app;
    }

    private static async Task<IResult> HandleAsync(ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetWorkLimitsQuery(), cancellationToken);
        return result.ToApiResult();
    }
}
