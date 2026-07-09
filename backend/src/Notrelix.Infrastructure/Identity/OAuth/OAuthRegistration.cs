using Notrelix.Application.Features.Identity.OAuth.Abstractions;
using Notrelix.Domain.Identity.OAuth;
using Notrelix.Infrastructure.Identity.OAuth;

namespace Notrelix.Infrastructure;

public static class OAuthRegistration
{
    public static IServiceCollection AddOAuthInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<OAuthOptions>()
            .Bind(configuration.GetSection("OAuth"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<OAuthOptions>, OAuthOptionsValidator>();

        services.AddHttpClient<IOAuthProviderClient, OAuthProviderClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        services.AddScoped<IOAuthStateStore, OAuthStateStore>();
        services.AddScoped<IOAuthOptionsProvider, OAuthOptionsProvider>();

        return services;
    }
}

internal sealed class OAuthOptionsProvider : IOAuthOptionsProvider
{
    private readonly OAuthOptions _options;

    public OAuthOptionsProvider(Microsoft.Extensions.Options.IOptions<OAuthOptions> options)
    {
        _options = options.Value;
    }

    public bool IsProviderEnabled(OAuthProvider provider)
    {
        var key = provider.ToString();
        return _options.Providers.TryGetValue(key, out var config) && config.Enabled;
    }

    public string GetRedirectUri(OAuthProvider provider)
    {
        var key = provider.ToString();
        return _options.Providers.TryGetValue(key, out var config)
            ? config.RedirectUri
            : throw new InvalidOperationException($"OAuth provider '{key}' is not configured.");
    }

    public string GetFrontendSuccessUrl() => _options.FrontendSuccessUrl;
    public string GetFrontendFailureUrl() => _options.FrontendFailureUrl;
    public string[] GetAllowedReturnOrigins() => [];
}
