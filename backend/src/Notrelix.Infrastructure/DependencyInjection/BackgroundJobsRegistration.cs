using Notrelix.Infrastructure.BackgroundJobs;
using Notrelix.Infrastructure.Messaging.Options;
using Notrelix.Infrastructure.Options;
using Notrelix.Infrastructure.Storage;

namespace Notrelix.Infrastructure;

/// <summary>
/// Background workers: outbox dispatcher, email dispatcher and attachment object reaper, security activity retention and the Chat retention and export jobs.
/// Business automation dispatch is durable outbox/broker-driven — no process-local queue.
/// </summary>
public static class BackgroundJobsRegistration
{
    /// <summary>
    /// Registers the worker's hosted services only when the <c>worker</c> role is selected. Options,
    /// validators and the scoped runners stay registered in every role: they start no background activity.
    /// </summary>
    public static IServiceCollection AddBackgroundJobs(
        this IServiceCollection services, IConfiguration configuration, RuntimeRoles? roles = null)
    {
        var runsWorker = (roles ?? RuntimeRoles.All).RunsWorker;
        void AddWorker<THosted>() where THosted : class, IHostedService
        {
            if (runsWorker)
            {
                services.AddHostedService<THosted>();
            }
        }

        services.AddOptions<OutboxDispatchOptions>()
            .Bind(configuration.GetSection(OutboxDispatchOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<OutboxDispatchOptions>, OutboxDispatchOptionsValidator>();
        AddWorker<OutboxDispatcher>();
        AddWorker<EmailDispatcher>();

        // Attachment object lifecycle: purge after retention + orphan reconciliation.
        services.AddOptions<AttachmentRetentionOptions>()
            .Bind(configuration.GetSection(AttachmentRetentionOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<AttachmentRetentionOptions>, AttachmentRetentionOptionsValidator>();
        services.AddScoped<IAttachmentReaperStore, PostgresAttachmentReaperStore>();
        services.AddScoped<AttachmentObjectReaper>();
        AddWorker<AttachmentObjectReaperService>();

        // Identity: remove profile photo objects that no user references (replaced, removed or never committed).
        services.AddScoped<IAvatarReaperStore, PostgresAvatarReaperStore>();
        services.AddScoped<AvatarObjectReaper>();
        AddWorker<AvatarObjectReaperService>();

        // User-visible security activity feed: delete entries past the retention (default 90 days).
        services.AddOptions<SecurityActivityRetentionOptions>()
            .Bind(configuration.GetSection(SecurityActivityRetentionOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<SecurityActivityRetentionOptions>, SecurityActivityRetentionOptionsValidator>();
        services.AddScoped<SecurityActivityRetentionSweeper>();
        AddWorker<SecurityActivityRetentionService>();

        // Governance: delete Board access-change rows past their 90-day retention (reads already hide them).
        services.AddOptions<ResourceAccessChangeRetentionOptions>()
            .Bind(configuration.GetSection(ResourceAccessChangeRetentionOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<ResourceAccessChangeRetentionOptions>, ResourceAccessChangeRetentionOptionsValidator>();
        services.AddScoped<ResourceAccessChangeRetentionSweeper>();
        AddWorker<ResourceAccessChangeRetentionService>();

        // Workspaces: delete onboarding drafts past their 30-day expiry (reads already treat them as absent).
        services.AddOptions<OnboardingDraftSweepOptions>()
            .Bind(configuration.GetSection(OnboardingDraftSweepOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<OnboardingDraftSweepOptions>, OnboardingDraftSweepOptionsValidator>();
        services.AddScoped<OnboardingDraftSweeper>();
        AddWorker<OnboardingDraftSweepService>();

        // Workspaces: purge the data of workspaces deleted more than 30 days ago (restore is over by then).
        services.AddOptions<DeletedWorkspacePurgeOptions>()
            .Bind(configuration.GetSection(DeletedWorkspacePurgeOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<DeletedWorkspacePurgeOptions>, DeletedWorkspacePurgeOptionsValidator>();
        services.AddScoped<DeletedWorkspacePurger>();
        AddWorker<DeletedWorkspacePurgeService>();

        // Work Management: purge the boards deleted more than 30 days ago (restore is over by then). Off by default.
        services.AddOptions<DeletedBoardPurgeOptions>()
            .Bind(configuration.GetSection(DeletedBoardPurgeOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<DeletedBoardPurgeOptions>, DeletedBoardPurgeOptionsValidator>();
        services.AddScoped<DeletedBoardPurger>();
        AddWorker<DeletedBoardPurgeService>();

        // Collaboration: tell assignees about Items due tomorrow and overdue Items, once per Item and due day.
        services.AddOptions<DueDateNotificationOptions>()
            .Bind(configuration.GetSection(DueDateNotificationOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<DueDateNotificationOptions>, DueDateNotificationOptionsValidator>();
        services.AddScoped<DueDateNotificationSweeper>();
        AddWorker<DueDateNotificationService>();

        // Work Management: run queued board file imports and exports, and remove expired export files.
        services.AddScoped<BoardTransferRunner>();
        AddWorker<BoardTransferService>();

        // Chat: apply each Workspace's retention rule (UC-CHT-H05, Chat.Retention.Apply) and build the exports of a person's chat data (UC-CHT-H06).
        services.AddOptions<ChatRetentionOptions>()
            .Bind(configuration.GetSection(ChatRetentionOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<ChatRetentionOptions>, ChatRetentionOptionsValidator>();
        services.AddScoped<Notrelix.Application.Features.Chat.Audit.Services.IChatSystemAudit, ChatSystemAudit>();
        services.AddScoped<Notrelix.Application.Features.Chat.Retention.Services.ChatRetentionApplier>();
        services.AddScoped<ChatRetentionRunner>();
        AddWorker<ChatRetentionService>();
        services.AddScoped<Notrelix.Application.Features.Chat.Exports.ChatExportBuilder>();
        services.AddScoped<ChatExportRunner>();
        AddWorker<ChatExportService>();
        services.AddOptions<ChatDigestOptions>()
            .Bind(configuration.GetSection(ChatDigestOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<ChatDigestOptions>, ChatDigestOptionsValidator>();
        services.AddScoped<Notrelix.Infrastructure.Notifications.ChatDigest.ChatDigestComposer>();
        services.AddScoped<ChatDigestRunner>();
        AddWorker<ChatDigestService>();

        return services;
    }
}
