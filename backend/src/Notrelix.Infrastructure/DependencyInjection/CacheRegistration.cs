using Notrelix.Application.Common.Abstractions;
using Notrelix.Infrastructure.Caching;

namespace Notrelix.Infrastructure;

/// <summary>
/// Redis connection, distributed cache and cache service.
/// </summary>
public static class CacheRegistration
{
    public static IServiceCollection AddCaching(
        this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Redis connection string is missing");

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var configuration = ConfigurationOptions.Parse(redisConnectionString);
            configuration.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(configuration);
        });

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "Notrelix_";
        });

        services.AddSingleton<IRedisCacheService, RedisCacheService>();

        return services;
    }
}
