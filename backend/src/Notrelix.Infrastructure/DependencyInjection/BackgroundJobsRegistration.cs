using Notrelix.Infrastructure.BackgroundJobs;
using Notrelix.Infrastructure.Messaging.Options;
using Notrelix.Infrastructure.Storage;

namespace Notrelix.Infrastructure;

/// <summary>
/// Background workers: outbox dispatcher, email dispatcher and attachment object reaper and security activity retention.
/// Business automation dispatch is durable outbox/broker-driven — no process-local queue.
/// </summary>
public static class BackgroundJobsRegistration
{
    public static IServiceCollection AddBackgroundJobs(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<OutboxDispatchOptions>()
            .Bind(configuration.GetSection(OutboxDispatchOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<OutboxDispatchOptions>, OutboxDispatchOptionsValidator>();
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

        // Identity: remove profile photo objects that no user references (replaced, removed or never committed).
        services.AddScoped<IAvatarReaperStore, PostgresAvatarReaperStore>();
        services.AddScoped<AvatarObjectReaper>();
        services.AddHostedService<AvatarObjectReaperService>();

        // User-visible security activity feed: delete entries past the retention (default 90 days).
        services.AddOptions<SecurityActivityRetentionOptions>()
            .Bind(configuration.GetSection(SecurityActivityRetentionOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<SecurityActivityRetentionOptions>, SecurityActivityRetentionOptionsValidator>();
        services.AddScoped<SecurityActivityRetentionSweeper>();
        services.AddHostedService<SecurityActivityRetentionService>();

        // Governance: delete Board access-change rows past their 90-day retention (reads already hide them).
        services.AddOptions<ResourceAccessChangeRetentionOptions>()
            .Bind(configuration.GetSection(ResourceAccessChangeRetentionOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<ResourceAccessChangeRetentionOptions>, ResourceAccessChangeRetentionOptionsValidator>();
        services.AddScoped<ResourceAccessChangeRetentionSweeper>();
        services.AddHostedService<ResourceAccessChangeRetentionService>();

        // Workspaces: delete onboarding drafts past their 30-day expiry (reads already treat them as absent).
        services.AddOptions<OnboardingDraftSweepOptions>()
            .Bind(configuration.GetSection(OnboardingDraftSweepOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<OnboardingDraftSweepOptions>, OnboardingDraftSweepOptionsValidator>();
        services.AddScoped<OnboardingDraftSweeper>();
        services.AddHostedService<OnboardingDraftSweepService>();

        // Workspaces: purge the data of workspaces deleted more than 30 days ago (restore is over by then).
        services.AddOptions<DeletedWorkspacePurgeOptions>()
            .Bind(configuration.GetSection(DeletedWorkspacePurgeOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<DeletedWorkspacePurgeOptions>, DeletedWorkspacePurgeOptionsValidator>();
        services.AddScoped<DeletedWorkspacePurger>();
        services.AddHostedService<DeletedWorkspacePurgeService>();

        // Collaboration: tell assignees about Items due tomorrow and overdue Items, once per Item and due day.
        services.AddOptions<DueDateNotificationOptions>()
            .Bind(configuration.GetSection(DueDateNotificationOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<DueDateNotificationOptions>, DueDateNotificationOptionsValidator>();
        services.AddScoped<DueDateNotificationSweeper>();
        services.AddHostedService<DueDateNotificationService>();

        // Work Management: run queued board file imports and exports, and remove expired export files.
        services.AddScoped<BoardTransferRunner>();
        services.AddHostedService<BoardTransferService>();

        return services;
    }
}
