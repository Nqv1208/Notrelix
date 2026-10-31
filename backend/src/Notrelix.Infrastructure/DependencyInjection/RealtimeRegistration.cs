using Notrelix.Application.Common.Realtime.Gateway;
using Notrelix.Infrastructure.Messaging;
using Notrelix.Infrastructure.Realtime;

namespace Notrelix.Infrastructure;

public static class RealtimeRegistration
{
    public static IServiceCollection AddRealtime(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment? environment = null)
    {
        var forceRedis = string.Equals(configuration["Realtime:Transport"], "Redis", StringComparison.OrdinalIgnoreCase);
        if (IsDevelopmentOrTesting(configuration, environment) && !forceRedis)
        {
            services.AddSingleton<InMemoryRealtimeNotificationSource>();
            services.AddSingleton<IRealtimeNotificationSource>(
                provider => provider.GetRequiredService<InMemoryRealtimeNotificationSource>());
            // Testing: no transport side effects. Development: deliver to this process's sockets.
            if (IsTesting(configuration, environment))
                services.AddScoped<IRealtimePublisher, DevNullRealtimePublisher>();
            else
                services.AddScoped<IRealtimePublisher, InProcessRealtimePublisher>();
        }
        else
        {
            // Production (or Realtime:Transport=Redis): real Redis-backed publisher (envelope contract in
            // RedisRealtimePublisher). Requires the Redis connection registered
            // by AddCaching; construction never connects (lazy multiplexer).
            services.AddScoped<IRealtimePublisher, RedisRealtimePublisher>();
            services.AddSingleton<IRealtimeNotificationSource, RedisRealtimeNotificationSource>();
        }

        return services;
    }

    private static bool IsTesting(IConfiguration configuration, IHostEnvironment? environment)
    {
        var env = environment?.EnvironmentName
               ?? configuration["DOTNET_ENVIRONMENT"]
               ?? configuration["ASPNETCORE_ENVIRONMENT"];
        return string.Equals(env, "Testing", StringComparison.OrdinalIgnoreCase);
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
