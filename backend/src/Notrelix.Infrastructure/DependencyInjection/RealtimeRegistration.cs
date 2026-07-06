using Notrelix.Infrastructure.Messaging;

namespace Notrelix.Infrastructure;

public static class RealtimeRegistration
{
    public static IServiceCollection AddRealtime(
        this IServiceCollection services, IConfiguration configuration)
    {
        // Dev fallback until a real IRealtimePublisher is implemented.
        services.AddScoped<IRealtimePublisher, DevNullRealtimePublisher>();

        return services;
    }
}
