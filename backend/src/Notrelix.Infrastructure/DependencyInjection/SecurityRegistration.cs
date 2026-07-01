using Notrelix.Application.Common.Abstractions;
using Notrelix.Infrastructure.RateLimiting;
using Notrelix.Infrastructure.Security.Encryption;
using Notrelix.Infrastructure.Security.Otp;

namespace Notrelix.Infrastructure;

public static class SecurityRegistration
{
    public static IServiceCollection AddSecurityInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IOtpService, OtpService>();
        services.AddSingleton<IRateLimitService, RedisRateLimitService>();
        services.AddSingleton<ISecretEncryptor, SecretEncryptor>();

        return services;
    }
}
