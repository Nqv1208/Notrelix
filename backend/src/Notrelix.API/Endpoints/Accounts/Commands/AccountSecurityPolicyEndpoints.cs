using Notrelix.API.Contracts.Accounts.Requests;
using Notrelix.API.Extensions;
using Microsoft.Extensions.Options;
using Notrelix.API.Options;
using Notrelix.Application.Features.Accounts.Security.Commands.SetAccountMfaPolicy;
using Notrelix.Application.Features.Accounts.Security.Queries.GetAccountSecurityPolicy;
using Notrelix.Application.Features.Accounts.Sso.Commands.AddSsoDomain;
using Notrelix.Application.Features.Accounts.Sso.Commands.RemoveSsoDomain;
using Notrelix.Application.Features.Accounts.Sso.Commands.RemoveSsoProvider;
using Notrelix.Application.Features.Accounts.Sso.Commands.SaveSsoProvider;
using Notrelix.Application.Features.Accounts.Sso.Commands.SetSsoRequired;
using Notrelix.Application.Features.Accounts.Sso.Commands.VerifySsoDomain;
using Notrelix.Application.Features.Accounts.Sso.Queries.GetAccountSso;

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

        // Enterprise single sign-on (OIDC): every one is Owner only (ManageAccountSecurity), the read included.
        group.MapAccountGet("/security-policy/sso", GetSsoAsync)
            .WithName("Accounts.GetSso")
            .WithTags("Accounts")
            .WithSummary("Read the account's single sign-on setup (Owner only; never returns the client secret)");
        group.MapAccountPut("/security-policy/sso/provider", SaveSsoProviderAsync)
            .WithName("Accounts.SaveSsoProvider")
            .WithTags("Accounts")
            .WithSummary("Save and validate the account's OIDC provider (Owner only)");
        group.MapAccountDelete("/security-policy/sso/provider", RemoveSsoProviderAsync)
            .WithName("Accounts.RemoveSsoProvider")
            .WithTags("Accounts")
            .WithSummary("Remove the account's OIDC provider (Owner only)");
        group.MapAccountPost("/security-policy/sso/domains", AddSsoDomainAsync)
            .WithName("Accounts.AddSsoDomain")
            .WithTags("Accounts")
            .WithSummary("Claim a domain and get its DNS TXT instructions (Owner only)");
        group.MapAccountPost("/security-policy/sso/domains/{id:guid}/verify", VerifySsoDomainAsync)
            .WithName("Accounts.VerifySsoDomain")
            .WithTags("Accounts")
            .WithSummary("Check the domain's DNS TXT record and verify it (Owner only)");
        group.MapAccountDelete("/security-policy/sso/domains/{id:guid}", RemoveSsoDomainAsync)
            .WithName("Accounts.RemoveSsoDomain")
            .WithTags("Accounts")
            .WithSummary("Remove a domain claim (Owner only)");
        group.MapAccountPut("/security-policy/sso/require", SetSsoRequiredAsync)
            .WithName("Accounts.SetSsoRequired")
            .WithTags("Accounts")
            .WithSummary("Require single sign-on for the members of the verified domains (Owner only)");
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

    private static async Task<IResult> GetSsoAsync(ISender sender, IOptions<SsoRedirectOptions> options, HttpRequest http)
    {
        var result = await sender.Send(new GetAccountSsoQuery());
        if (!result.Succeeded) return result.ToApiResult();
        return Results.Ok(result.Data! with { RedirectUri = options.Value.CallbackUrl(http) });
    }

    private static async Task<IResult> SaveSsoProviderAsync(SaveSsoProviderRequest request, ISender sender)
    {
        var result = await sender.Send(new SaveSsoProviderCommand(
            request.Name, request.IssuerUrl, request.ClientId, request.ClientSecret, request.JitProvisioning));
        return result.ToApiResult();
    }

    private static async Task<IResult> RemoveSsoProviderAsync(ISender sender)
    {
        var result = await sender.Send(new RemoveSsoProviderCommand());
        return result.ToNoContentResult();
    }

    private static async Task<IResult> AddSsoDomainAsync(AddSsoDomainRequest request, ISender sender)
    {
        var result = await sender.Send(new AddSsoDomainCommand(request.Domain));
        return result.ToCreatedResult(result.Data is null ? null : $"/api/v1/accounts/security-policy/sso/domains/{result.Data.Id}");
    }

    private static async Task<IResult> VerifySsoDomainAsync(Guid id, ISender sender)
    {
        var result = await sender.Send(new VerifySsoDomainCommand(id));
        return result.ToApiResult();
    }

    private static async Task<IResult> RemoveSsoDomainAsync(Guid id, ISender sender)
    {
        var result = await sender.Send(new RemoveSsoDomainCommand(id));
        return result.ToNoContentResult();
    }

    private static async Task<IResult> SetSsoRequiredAsync(SetSsoRequiredRequest request, ISender sender)
    {
        var result = await sender.Send(new SetSsoRequiredCommand(request.Required));
        return result.ToNoContentResult();
    }
}
