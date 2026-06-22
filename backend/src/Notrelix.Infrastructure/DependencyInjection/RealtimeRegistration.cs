using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Infrastructure.Messaging;
using Notrelix.Infrastructure.Realtime.Publishers;

namespace Notrelix.Infrastructure;

public static class RealtimeRegistration
{
    public static IServiceCollection AddRealtime(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<INotificationService, RedisNotificationService>();

        // Dev fallback until a real IRealtimePublisher is implemented.
        services.AddScoped<IRealtimePublisher, DevNullRealtimePublisher>();

        return services;
    }
}
