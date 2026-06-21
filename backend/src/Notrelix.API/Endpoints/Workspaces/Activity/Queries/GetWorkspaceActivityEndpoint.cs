using MediatR;
using Notrelix.API.Extensions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.WorkspaceHome.Queries.GetWorkspaceActivity;

namespace Notrelix.API.Endpoints.Workspaces.Activity.Queries;

public static class GetWorkspaceActivityEndpoint
{
    public static IEndpointRouteBuilder MapGetWorkspaceActivity(this IEndpointRouteBuilder group)
    {
        group.MapGet("/", HandleAsync)
            .WithName("Workspaces.Activity.GetWorkspaceActivity")
            .WithTags("Workspaces.Activity")
            .WithSummary("Get workspace activity log");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        ISender sender,
        int page = 1,
        int pageSize = 20)
    {
        var result = await sender.Send(new GetWorkspaceActivityQuery(workspaceId, page, pageSize));
        return result.ToApiResult();
    }
}
