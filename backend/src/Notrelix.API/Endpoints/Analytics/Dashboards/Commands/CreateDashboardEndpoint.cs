using Notrelix.API.Contracts.Analytics.Dashboards.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Analytics.Dashboards.Commands.CreateDashboard;

namespace Notrelix.API.Endpoints.Analytics.Dashboards.Commands;

public static class CreateDashboardEndpoint
{
    public static IEndpointRouteBuilder MapCreateDashboard(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePost("/", HandleAsync)
            .WithName("Analytics.Dashboards.CreateDashboard")
            .WithTags("Analytics")
            .WithIdempotencyKey()
            .WithSummary("Create an empty private dashboard owned by the caller")
            .Produces<Guid>(StatusCodes.Status201Created, "application/json");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        CreateDashboardRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateDashboardCommand(workspaceId, request.Name), cancellationToken);
        return result.ToCreatedResult();
    }
}
