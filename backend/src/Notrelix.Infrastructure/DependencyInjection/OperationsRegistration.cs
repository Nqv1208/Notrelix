using Notrelix.Infrastructure.Operations.Idempotency;

namespace Notrelix.Infrastructure;

public static class OperationsRegistration
{
    public static IServiceCollection AddOperations(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment? environment = null)
    {
        // Production always registers the real PostgreSQL-backed store.
        // Tests that need a fake must override explicitly in their composition root.
        services.AddScoped<IIdempotencyStore, EfIdempotencyStore>();

        return services;
    }
}
