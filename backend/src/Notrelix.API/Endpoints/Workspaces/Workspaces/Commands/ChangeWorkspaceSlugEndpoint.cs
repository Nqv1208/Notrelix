using Notrelix.API.Contracts.Workspaces.Workspaces.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.ChangeWorkspaceSlug;

namespace Notrelix.API.Endpoints.Workspaces.Workspaces.Commands;

public static class ChangeWorkspaceSlugEndpoint
{
    public static IEndpointRouteBuilder MapChangeWorkspaceSlug(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePatch("/slug", HandleAsync)
            .WithName("Workspaces.Workspaces.ChangeWorkspaceSlug")
            .WithTags("Workspaces.Workspaces")
            .WithSummary("Change the workspace address")
            .WithDescription(
                "Owner only. Send the workspace version you read as expectedVersion: a stale version answers 412 and changes nothing. " +
                "The address follows the same rule as creation (3 to 63 characters, lowercase letters, numbers and single hyphens, not reserved) and " +
                "is unique within the Account: a taken one answers 409 workspaces.workspace.slug-taken. The same address is a no-op: no version bump, no event. " +
                "Links use ids, so the old address is released and does not redirect. An archived workspace refuses the change.");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        ChangeWorkspaceSlugRequest request,
        ISender sender)
    {
        var result = await sender.Send(new ChangeWorkspaceSlugCommand(workspaceId, request.Slug, request.ExpectedVersion));
        return result.ToApiResult();
    }
}
