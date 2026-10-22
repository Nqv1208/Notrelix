using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Notrelix.API.Realtime;

public static class RealtimeGatewayRegistration
{
    public static IServiceCollection AddRealtimeGateway(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        services.AddOptions<RealtimeGatewayOptions>()
            .Bind(configuration.GetSection(RealtimeGatewayOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<RealtimeGatewayOptions>, RealtimeGatewayOptionsValidator>();

        // The gateway reuses the CORS allow-list unless it is overridden explicitly.
        var gatewayOrigins = configuration.GetSection($"{RealtimeGatewayOptions.SectionName}:AllowedOrigins").Get<string[]>() ?? [];
        var origins = gatewayOrigins.Length > 0
            ? gatewayOrigins
            : configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        var allowWildcard = environment.IsDevelopment() || environment.IsEnvironment("Testing");
        services.AddSingleton(new RealtimeOriginPolicy(origins, allowWildcard));

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<RealtimeConnectionLimiter>();
        services.AddSingleton<IRealtimeSubscriptionAuthorizer, MediatorRealtimeSubscriptionAuthorizer>();
        services.AddSingleton<IRealtimeCredentialValidator, JwtRealtimeCredentialValidator>();

        return services;
    }
}
