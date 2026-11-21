using Notrelix.API.Contracts.Accounts.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Accounts.Security.Commands.SetAccountMfaPolicy;
using Notrelix.Application.Features.Accounts.Security.Queries.GetAccountSecurityPolicy;

namespace Notrelix.API.Endpoints.Accounts.Commands;

public static class AccountSecurityPolicyEndpoints
{
    public static IEndpointRouteBuilder MapAccountSecurityPolicy(this IEndpointRouteBuilder group)
    {
        group.MapAccountGet("/security-policy", GetAsync)
            .WithName("Accounts.GetSecurityPolicy")
            .WithTags("Accounts")
            .WithSummary("Read the current account's security policy");
        group.MapAccountPut("/security-policy/mfa", SetMfaAsync)
            .WithName("Accounts.SetMfaPolicy")
            .WithTags("Accounts")
            .WithSummary("Require MFA for every member of the current account (Owner only)");
        return group;
    }

    private static async Task<IResult> GetAsync(ISender sender)
    {
        var result = await sender.Send(new GetAccountSecurityPolicyQuery());
        return result.ToApiResult();
    }

    private static async Task<IResult> SetMfaAsync(SetAccountMfaPolicyRequest request, ISender sender)
    {
        var result = await sender.Send(new SetAccountMfaPolicyCommand(request.Required));
        return result.ToNoContentResult();
    }
}
