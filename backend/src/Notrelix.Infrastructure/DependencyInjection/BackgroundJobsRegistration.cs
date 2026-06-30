using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Infrastructure.BackgroundJobs;

namespace Notrelix.Infrastructure;

/// <summary>
/// Background workers: job queue, queued job worker and outbox dispatcher.
/// </summary>
public static class BackgroundJobsRegistration
{
    public static IServiceCollection AddBackgroundJobs(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<InMemoryJobQueue>();
        services.AddSingleton<IJobQueue>(sp => sp.GetRequiredService<InMemoryJobQueue>());
        services.AddSingleton<IBackgroundJobQueueReader>(sp => sp.GetRequiredService<InMemoryJobQueue>());

        services.AddScoped<N8nDispatchService>();

        services.AddHostedService<QueuedJobWorker>();
        services.AddHostedService<OutboxDispatcher>();
        services.AddHostedService<V5OutboxDispatcher>();
        services.AddHostedService<EmailDispatcher>();

        return services;
    }
}
