using Microsoft.AspNetCore.Authentication;

namespace Notrelix.Infrastructure.Auth.ApiTokens;

/// <summary>
/// Options for the API token authentication scheme. Additive to the default
/// JWT bearer scheme; consumers opt in explicitly.
/// </summary>
public sealed class ApiTokenAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string SchemeName = "ApiToken";
}