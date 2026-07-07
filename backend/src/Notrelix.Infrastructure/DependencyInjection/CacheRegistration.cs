using Notrelix.Infrastructure.Caching;
using Notrelix.Infrastructure.Options;

namespace Notrelix.Infrastructure;

/// <summary>
/// Redis connection, distributed cache, cache service, and cache key factory.
/// </summary>
public static class CacheRegistration
{
    public static IServiceCollection AddCaching(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment? environment = null)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Redis connection string is missing");

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var redisConfig = ConfigurationOptions.Parse(redisConnectionString);
            redisConfig.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(redisConfig);
        });

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "Notrelix_";
        });

        services.AddSingleton<IRedisCacheService, RedisCacheService>();

        services.AddOptions<CacheKeyOptions>()
            .Bind(configuration.GetSection("CacheKey"))
            .PostConfigure(options =>
            {
                if (environment is not null)
                    options.Environment = environment.EnvironmentName;
            })
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<CacheKeyOptions>, CacheKeyOptionsValidator>();
        services.AddSingleton<CacheKeyFactory>();

        return services;
    }
}
