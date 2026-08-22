using Notrelix.API.Extensions;
using Notrelix.Application.Features.Identity.Mfa.Queries.GetMfaStatus;

namespace Notrelix.API.Endpoints.Identity.Auth.Queries;

public static class GetMfaStatusEndpoint
{
    public static IEndpointRouteBuilder MapGetMfaStatus(this IEndpointRouteBuilder group)
    {
        group.MapAuthenticatedGet("/mfa/status", HandleAsync)
            .WithName("Identity.Mfa.Status")
            .WithTags("Identity.Mfa")
            .WithSummary("Get MFA status for the authenticated user");
        return group;
    }

    private static async Task<IResult> HandleAsync(ISender sender)
    {
        var result = await sender.Send(new GetMfaStatusQuery());
        return result.ToApiResult();
    }
}