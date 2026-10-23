using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Onboarding.Queries.CheckWorkspaceSlug;

namespace Notrelix.API.Endpoints.Workspaces.Workspaces.Queries;

public static class CheckWorkspaceSlugEndpoint
{
    public static IEndpointRouteBuilder MapCheckWorkspaceSlug(this IEndpointRouteBuilder group)
    {
        group.MapAccountGet("/slug-check", HandleAsync)
            .WithName("Workspaces.Workspaces.CheckWorkspaceSlug")
            .WithTags("Workspaces.Workspaces")
            .WithSummary("Check whether a workspace address is free in the account")
            .WithDescription("Status is free, taken or invalid (not a lowercase letters, digits and hyphens slug of at most 128 characters). A slug is unique within the account only. When taken, suggestion is the next free address. Needs the permission to create a workspace in the account.")
            .Produces<WorkspaceSlugCheckDto>(StatusCodes.Status200OK, "application/json");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid accountId,
        ISender sender,
        string? slug = null)
    {
        var result = await sender.Send(new CheckWorkspaceSlugQuery(slug));
        return result.ToApiResult();
    }
}
