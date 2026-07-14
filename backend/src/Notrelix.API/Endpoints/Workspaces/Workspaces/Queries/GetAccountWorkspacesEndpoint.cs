using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Workspaces.Queries.GetAccountWorkspaces;

namespace Notrelix.API.Endpoints.Workspaces.Workspaces.Queries;

public static class GetAccountWorkspacesEndpoint
{
    public static IEndpointRouteBuilder MapGetAccountWorkspaces(this IEndpointRouteBuilder group)
    {
        group.MapAccountGet("/", HandleAsync)
            .WithName("Workspaces.Workspaces.GetAccountWorkspaces")
            .WithTags("Workspaces.Workspaces")
            .WithSummary("Get all workspaces for an account");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        ISender sender)
    {
        var result = await sender.Send(new GetAccountWorkspacesQuery());
        return result.ToApiResult();
    }
}
