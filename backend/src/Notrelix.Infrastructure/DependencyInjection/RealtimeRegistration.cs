using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Infrastructure.Realtime.Publishers;

namespace Notrelix.Infrastructure;

/// <summary>
/// Realtime delivery (currently Redis pub/sub based notification fan-out).
/// </summary>
public static class RealtimeRegistration
{
    public static IServiceCollection AddRealtime(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<INotificationService, RedisNotificationService>();

        return services;
    }
}
