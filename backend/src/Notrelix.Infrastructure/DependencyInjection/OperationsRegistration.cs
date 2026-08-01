using Notrelix.Application.Common.Idempotency;
using Notrelix.Infrastructure.Operations.Idempotency;
using Notrelix.Infrastructure.Ops;

namespace Notrelix.Infrastructure;

public static class OperationsRegistration
{
    public static IServiceCollection AddOperations(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment? environment = null)
    {
        var isTesting = environment?.IsEnvironment("Testing") == true;

        if (isTesting)
        {
            services.AddScoped<IIdempotencyStore, DevNullIdempotencyStore>();
        }
        else
        {
            services.AddScoped<IIdempotencyStore, EfIdempotencyStore>();
        }

        return services;
    }
}
