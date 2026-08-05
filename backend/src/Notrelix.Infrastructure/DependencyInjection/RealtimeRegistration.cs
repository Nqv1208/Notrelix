using Microsoft.Extensions.Hosting;
using Notrelix.Infrastructure.Messaging;
using Notrelix.Infrastructure.Realtime;

namespace Notrelix.Infrastructure;

public static class RealtimeRegistration
{
    public static IServiceCollection AddRealtime(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment? environment = null)
    {
        if (IsDevelopmentOrTesting(configuration, environment))
        {
            // Dev fallback: no transport side effects in development/testing.
            services.AddScoped<IRealtimePublisher, DevNullRealtimePublisher>();
        }
        else
        {
            // Production: real Redis-backed publisher (envelope contract in
            // RedisRealtimePublisher). Requires the Redis connection registered
            // by AddCaching; construction never connects (lazy multiplexer).
            services.AddScoped<IRealtimePublisher, RedisRealtimePublisher>();
        }

        return services;
    }

    private static bool IsDevelopmentOrTesting(IConfiguration configuration, IHostEnvironment? environment)
    {
        if (environment is not null)
        {
            return environment.IsDevelopment() || environment.IsEnvironment("Testing");
        }

        var env = configuration["DOTNET_ENVIRONMENT"]
               ?? configuration["ASPNETCORE_ENVIRONMENT"];
        return string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase)
            || string.Equals(env, "Testing", StringComparison.OrdinalIgnoreCase);
    }
}
