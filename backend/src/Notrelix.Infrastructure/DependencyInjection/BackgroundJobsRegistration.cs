using Notrelix.Infrastructure.BackgroundJobs;

namespace Notrelix.Infrastructure;

/// <summary>
/// Background workers: outbox dispatcher and email dispatcher.
/// Business automation dispatch is durable outbox/broker-driven — no process-local queue.
/// </summary>
public static class BackgroundJobsRegistration
{
    public static IServiceCollection AddBackgroundJobs(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHostedService<OutboxDispatcher>();
        services.AddHostedService<EmailDispatcher>();

        return services;
    }
}
