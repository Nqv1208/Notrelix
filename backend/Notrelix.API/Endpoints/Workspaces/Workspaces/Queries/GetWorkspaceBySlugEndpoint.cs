using MediatR;
using Microsoft.AspNetCore.Mvc;
using Notrelix.API.Extensions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Workspaces.Queries.GetWorkspaceBySlug;

namespace Notrelix.API.Endpoints.Workspaces.Workspaces.Queries;

public static class GetWorkspaceBySlugEndpoint
{
    public static IEndpointRouteBuilder MapGetWorkspaceBySlug(this IEndpointRouteBuilder group)
    {
        group.MapGet("/", HandleAsync)
            .WithName("Workspaces.Workspaces.GetWorkspaceBySlug")
            .WithTags("Workspaces.Workspaces")
            .WithSummary("Resolve a workspace by slug for legacy/deep-link migration");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        string slug,
        [FromHeader(Name = "X-Workspace-Id")] Guid workspaceId,
        ISender sender)
    {
        var result = await sender.Send(new GetWorkspaceBySlugQuery(workspaceId, slug));
        return result.ToApiResult();
    }
}
