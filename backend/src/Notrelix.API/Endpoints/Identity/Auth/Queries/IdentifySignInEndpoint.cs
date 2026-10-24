using Notrelix.API.Extensions;
using Notrelix.API.RateLimiting;
using Notrelix.Application.Features.Identity.Auth.Queries.IdentifySignIn;

namespace Notrelix.API.Endpoints.Identity.Auth.Queries;

public sealed record IdentifySignInRequest(string Email);

public static class IdentifySignInEndpoint
{
    public static IEndpointRouteBuilder MapIdentifySignIn(this IEndpointRouteBuilder group)
    {
        group.MapPublicPost("/identify", HandleAsync)
            .WithName("Identity.Auth.IdentifySignIn")
            .Produces<SignInMethodDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status429TooManyRequests)
            .WithTags("Identity.Auth")
            .WithSummary("Tell the sign-in page how an email signs in")
            .WithDescription("Answers password or single sign-on from the email's text alone; an unknown email and a known password email get the same answer. Rate limited per IP.")
            .WithMetadata(new RateLimitPolicyAttribute("AuthStrictByIp"));
        return group;
    }

    private static async Task<IResult> HandleAsync(
        IdentifySignInRequest request,
        ISender sender)
    {
        var result = await sender.Send(new IdentifySignInQuery(request.Email));
        return result.ToApiResult();
    }
}
