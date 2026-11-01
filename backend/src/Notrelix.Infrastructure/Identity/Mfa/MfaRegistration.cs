using Notrelix.Application.Features.Identity.Mfa.Abstractions;
using Notrelix.Application.Features.Identity.Auth.Throttling;
using Notrelix.Infrastructure.Identity.Auth;
using Notrelix.Infrastructure.Identity.Mfa;

namespace Notrelix.Infrastructure;

public static class MfaRegistration
{
    public static IServiceCollection AddMfaInfrastructure(
        this IServiceCollection services)
    {
        services.AddScoped<IMfaChallengeStore, MfaChallengeStore>();
        services.AddScoped<IMfaTotpService, MfaTotpService>();
        services.AddScoped<IMfaRecoveryCodeGenerator, MfaRecoveryCodeGenerator>();
        services.AddScoped<IMfaCodeUseGuard, MfaCodeUseGuard>();

        return services;
    }
}

public static class LoginThrottleRegistration
{
    public static IServiceCollection AddLoginThrottleInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<LoginThrottleOptions>()
            .Bind(configuration.GetSection(LoginThrottleOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<LoginThrottleOptions>, LoginThrottleOptionsValidator>();
        services.AddScoped<ILoginThrottle, RedisLoginThrottle>();

        return services;
    }
}
