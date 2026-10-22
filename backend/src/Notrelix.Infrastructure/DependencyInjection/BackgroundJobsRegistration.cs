using Notrelix.Infrastructure.BackgroundJobs;
using Notrelix.Infrastructure.Storage;

namespace Notrelix.Infrastructure;

/// <summary>
/// Background workers: outbox dispatcher, email dispatcher and attachment object reaper.
/// Business automation dispatch is durable outbox/broker-driven — no process-local queue.
/// </summary>
public static class BackgroundJobsRegistration
{
    public static IServiceCollection AddBackgroundJobs(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHostedService<OutboxDispatcher>();
        services.AddHostedService<EmailDispatcher>();

        // Attachment object lifecycle: purge after retention + orphan reconciliation.
        services.AddOptions<AttachmentRetentionOptions>()
            .Bind(configuration.GetSection(AttachmentRetentionOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<AttachmentRetentionOptions>, AttachmentRetentionOptionsValidator>();
        services.AddScoped<IAttachmentReaperStore, PostgresAttachmentReaperStore>();
        services.AddScoped<AttachmentObjectReaper>();
        services.AddHostedService<AttachmentObjectReaperService>();

        return services;
    }
}
