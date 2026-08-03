using Notrelix.API.Extensions;
using Notrelix.Application.Features.Identity.Auth.Queries.GetBootstrap;

namespace Notrelix.API.Endpoints.Identity.Auth.Queries;

public static class GetBootstrapEndpoint
{
    public static IEndpointRouteBuilder MapGetBootstrap(this IEndpointRouteBuilder group)
    {
        group.MapAuthenticatedGet("/bootstrap", HandleAsync)
            .WithName("Identity.Auth.GetBootstrap")
            .WithTags("Identity.Auth")
            .WithSummary("Get current user bootstrap state");
        return group;
    }

    private static async Task<IResult> HandleAsync(ISender sender)
    {
        var result = await sender.Send(new GetBootstrapQuery());
        return result.ToApiResult();
    }
}
