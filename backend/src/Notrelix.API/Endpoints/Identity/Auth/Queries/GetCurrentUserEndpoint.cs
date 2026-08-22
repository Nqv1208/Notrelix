using Notrelix.API.Extensions;
using Notrelix.Application.Features.Identity.Auth.Queries.GetCurrentUser;

namespace Notrelix.API.Endpoints.Identity.Auth.Queries;

public static class GetCurrentUserEndpoint
{
    public static IEndpointRouteBuilder MapGetCurrentUser(this IEndpointRouteBuilder group)
    {
        group.MapAuthenticatedGet("/me", HandleAsync)
            .WithName("Identity.Auth.GetCurrentUser")
            .WithTags("Identity.Auth")
            .WithSummary("Get current authenticated user");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        ISender sender)
    {
        var query = new GetCurrentUserQuery();
        var result = await sender.Send(query);
        return result.ToApiResult();
    }
}
