using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Infrastructure.Security.Otp;
using Notrelix.Infrastructure.Security.RateLimiting;

namespace Notrelix.Infrastructure;

/// <summary>
/// Security primitives: rate limiting and one-time passwords.
/// </summary>
public static class SecurityRegistration
{
    public static IServiceCollection AddSecurityInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IOtpService, OtpService>();
        services.AddSingleton<IRateLimitService, RateLimitService>();

        return services;
    }
}
