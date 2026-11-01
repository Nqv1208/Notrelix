using Notrelix.Application.Features.Identity.Mfa.DTOs;
using Notrelix.API.Extensions;
using Notrelix.API.RateLimiting;
using Notrelix.Application.Features.Identity.Mfa.Queries.GetMfaStatus;

namespace Notrelix.API.Endpoints.Identity.Auth.Queries;

public static class GetMfaStatusEndpoint
{
    public static IEndpointRouteBuilder MapGetMfaStatus(this IEndpointRouteBuilder group)
    {
        group.MapAuthenticatedGet("/mfa/status", HandleAsync)
            .WithName("Identity.Mfa.Status")
            .Produces<MfaStatusDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblems(StatusCodes.Status401Unauthorized, StatusCodes.Status429TooManyRequests)
            .WithTags("Identity.Mfa")
            .WithSummary("Get MFA status for the authenticated user")
            .WithDescription("The caller's own status only: whether a factor is on, its method and how many recovery codes are left. Never a secret or a code.")
            .WithMetadata(new RateLimitPolicyAttribute("GeneralAuthenticated"));
        return group;
    }

    private static async Task<IResult> HandleAsync(ISender sender)
    {
        var result = await sender.Send(new GetMfaStatusQuery());
        return result.ToApiResult();
    }
}