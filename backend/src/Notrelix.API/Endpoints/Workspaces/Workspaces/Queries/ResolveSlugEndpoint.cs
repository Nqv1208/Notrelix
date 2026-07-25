using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Workspaces.Queries.ResolveSlug;

namespace Notrelix.API.Endpoints.Workspaces.Workspaces.Queries;

public static class ResolveSlugEndpoint
{
    public static IEndpointRouteBuilder MapResolveSlug(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPublicGet("/api/v1/accounts/{accountId:guid}/resolve", HandleAsync)
            .WithName("Workspaces.Workspaces.ResolveSlug")
            .WithTags("Workspaces.Workspaces")
            .WithSummary("Resolve a workspace by account ID and slug");
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
