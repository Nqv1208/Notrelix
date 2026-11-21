using Notrelix.Application.Common.Oidc;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Infrastructure.Accounts.Sso;
using Notrelix.Infrastructure.Oidc;

namespace Notrelix.Infrastructure;

public static class SsoRegistration
{
    public static IServiceCollection AddSsoInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<OidcOutboundOptions>().Bind(configuration.GetSection(OidcOutboundOptions.SectionName));

        // The one guarded outbound path to company identity providers (discovery, token endpoint, JWKS).
        services.AddHttpClient(OidcGuardedHttp.ClientName)
            .ConfigurePrimaryHttpMessageHandler(sp =>
                OidcGuardedHttp.CreateHandler(sp.GetRequiredService<IOptions<OidcOutboundOptions>>().Value.AllowPrivateTargets))
            .ConfigureHttpClient(client => client.Timeout = OidcGuardedHttp.Timeout);
        services.AddScoped<IOidcDiscoveryClient, OidcDiscoveryClient>();

        services.AddSingleton<IDomainTxtResolver, DnsDomainTxtResolver>();
        services.AddScoped<IAccountSsoScopeReader, AccountSsoScopeReader>();
        return services;
    }
}
