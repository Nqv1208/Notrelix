using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Settings.Queries.GetWorkspaceSettings;

namespace Notrelix.API.Endpoints.Workspaces.Settings.Queries;

public static class GetWorkspaceSettingsEndpoint
{
    public static IEndpointRouteBuilder MapGetWorkspaceSettings(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceGet("/", HandleAsync)
            .WithName("Workspaces.Settings.GetWorkspaceSettings")
            .WithTags("Workspaces.Settings")
            .WithSummary("Get workspace settings");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        ISender sender)
    {
        var result = await sender.Send(new GetWorkspaceSettingsQuery(workspaceId));
        return result.ToApiResult();
    }
}
