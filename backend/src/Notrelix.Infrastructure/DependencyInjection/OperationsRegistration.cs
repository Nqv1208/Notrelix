using Notrelix.Infrastructure.Ops;

namespace Notrelix.Infrastructure;

public static class OperationsRegistration
{
    public static IServiceCollection AddOperations(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IIdempotencyStore,
            DevNullIdempotencyStore>();
        return services;
    }
}
