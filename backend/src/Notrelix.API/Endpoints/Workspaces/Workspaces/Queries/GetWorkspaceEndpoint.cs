using MediatR;
using Notrelix.API.Extensions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Workspaces.Queries.GetWorkspace;

namespace Notrelix.API.Endpoints.Workspaces.Workspaces.Queries;

public static class GetWorkspaceEndpoint
{
    public static IEndpointRouteBuilder MapGetWorkspace(this IEndpointRouteBuilder group)
    {
        group.MapGet("/", HandleAsync)
            .WithName("Workspaces.Workspaces.GetWorkspace")
            .WithTags("Workspaces.Workspaces")
            .WithSummary("Get workspace by ID");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        ISender sender)
    {
        var result = await sender.Send(new GetWorkspaceQuery(workspaceId));
        return result.ToApiResult();
    }
}
