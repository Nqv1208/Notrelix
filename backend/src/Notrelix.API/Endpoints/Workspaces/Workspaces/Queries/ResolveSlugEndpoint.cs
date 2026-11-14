using Notrelix.Application.Features.Workspaces.DTOs;
using Notrelix.API.Extensions;
using Notrelix.API.RateLimiting;
using Notrelix.Application.Features.Workspaces.Workspaces.Queries.ResolveSlug;

namespace Notrelix.API.Endpoints.Workspaces.Workspaces.Queries;

public static class ResolveSlugEndpoint
{
    public static IEndpointRouteBuilder MapResolveSlug(this IEndpointRouteBuilder endpoints)
    {
        // Anonymous, so it is limited per IP (GeneralAnonymous) to slow down enumeration of addresses (WGREQ032).
        endpoints.MapPublicGet("/api/v1/accounts/{accountId:guid}/resolve", HandleAsync)
            .WithName("Workspaces.Workspaces.ResolveSlug")
            .WithTags("Workspaces.Workspaces")
            .WithSummary("Resolve a workspace by account ID and slug")
            .WithMetadata(new RateLimitPolicyAttribute("GeneralAnonymous"))
            .Produces<WorkspaceDto>(StatusCodes.Status200OK, "application/json");
        return endpoints;
    }

    private static async Task<IResult> HandleAsync(
        Guid accountId,
        ISender sender,
        string? slug = null)
    {
        var result = await sender.Send(new ResolveSlugQuery(accountId, slug));
        return result.ToApiResult();
    }
}
