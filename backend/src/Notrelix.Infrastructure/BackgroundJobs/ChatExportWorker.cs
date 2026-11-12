using Notrelix.Application.Common.SystemOperations;
using Notrelix.Application.Features.Chat.Audit.Services;
using Notrelix.Application.Features.Chat.Exports;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Exports;
using Notrelix.Domain.Chat.Rules;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.BackgroundJobs;

/// <summary>
/// Runs the queued exports of a person's chat data (UC-CHT-H06) and removes the files that expired. One job at a time, claimed with
/// <c>FOR UPDATE SKIP LOCKED</c> so several instances never build the same export. The build runs under the explicit system (worker) context
/// for the person who asked: it checks that they still are an active Owner or Admin before it reads a row, builds the file bounded by the
/// size limits, stores it through the platform object storage and records the counts and one Audit fact. A failure stores no file and ends
/// the job as failed with a code; the file of a ready export is deleted when it expires (24 hours).
/// </summary>
internal sealed class ChatExportRunner(
    ApplicationDbContext context,
    ICurrentTenantContext tenant,
    IRlsSessionContext rls,
    IStorageService storage,
    ChatExportBuilder builder,
    IWorkspaceMemberFacts members,
    IChatSystemAudit audit,
    IDateTimeProvider clock,
    ILogger<ChatExportRunner> logger)
{
    private static readonly SystemOperationReason Reason =
        new("Chat", "Build the export of a person's chat data for the Owner or Admin who asked, and remove expired export files");

    public const string NoAccessFailure = "no-access";
    public const string ErrorFailure = "error";
    private static readonly TimeSpan StaleRunning = TimeSpan.FromMinutes(15);

    /// <returns>True when a job was run, so the caller looks for the next one at once.</returns>
    public async Task<bool> RunNextAsync(CancellationToken cancellationToken)
    {
        ChatExportJob? job;
        using (new SystemContextScope(tenant, logger, "chat-export.claim", Reason))
        {
            await using var claim = await context.Database.BeginTransactionAsync(cancellationToken);
            await rls.ApplyAsync(cancellationToken);
            job = (await context.ChatExportJobs
                .FromSql($"""
                    SELECT * FROM chat.export_jobs
                    WHERE status = 'queued'
                    ORDER BY created_at
                    LIMIT 1
                    FOR UPDATE SKIP LOCKED
                    """)
                .ToListAsync(cancellationToken)).SingleOrDefault();
            if (job is null)
            {
                await claim.RollbackAsync(cancellationToken);
                return false;
            }

            job.Start(clock.UtcNow);
            await context.SaveChangesAsync(cancellationToken);
            await claim.CommitAsync(cancellationToken);
        }

        try
        {
            await BuildAsync(job, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Chat export {JobId} failed", job.Id);
            context.ChangeTracker.Clear();
            await FailAsync(job.Id, ErrorFailure, cancellationToken);
        }

        return true;
    }

    private async Task BuildAsync(ChatExportJob job, CancellationToken cancellationToken)
    {
        string? key = null;
        using var system = new SystemContextScope(tenant, logger, "chat-export.build", Reason);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await rls.ApplyAsync(cancellationToken);
        try
        {
            var requester = await members.FindAsync(job.WorkspaceId, job.RequestedBy, cancellationToken);
            var mayExport = requester is { IsActive: true, Role: "Owner" or "Admin" };
            if (!mayExport)
            {
                job.Fail(NoAccessFailure, clock.UtcNow);
                await Record(job, "failed", new ChatExportBuild(0, 0, 0, 0, NoAccessFailure, 0), cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return;
            }

            var now = clock.UtcNow;
            using var file = new MemoryStream();
            var result = await builder.BuildAsync(job, now, file, cancellationToken);
            if (result.IsTooLarge)
            {
                job.Fail(result.FailureCode!, now);
                await Record(job, "failed", result, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return;
            }

            key = ChatExportFiles.StorageKey(job);
            file.Position = 0;
            await storage.WriteAsync(key, file, ChatExportFiles.ContentType, cancellationToken);

            job.Complete(
                key, file.Length, result.Messages, result.Spaces, result.ExcludedSpaces, result.Tombstones, now,
                TimeSpan.FromHours(ChatLimits.ExportLifetimeHours));
            await Record(job, "ready", result with { SizeBytes = file.Length }, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            // The file is stored before the row commits; if the commit does not happen the file would be an orphan, so it goes.
            if (key is not null)
                await storage.DeleteAsync(key, CancellationToken.None);
            throw;
        }
    }

    private Task Record(ChatExportJob job, string outcome, ChatExportBuild result, CancellationToken cancellationToken) =>
        audit.RecordExportCompletedAsync(
            new ExportCompletedFact(
                job.AccountId, job.WorkspaceId, job.Id, job.RequestedBy, job.SubjectUserId, outcome,
                result.Messages, result.Spaces, result.ExcludedSpaces, result.Tombstones, result.SizeBytes),
            cancellationToken);

    private async Task FailAsync(Guid jobId, string code, CancellationToken cancellationToken)
    {
        using var system = new SystemContextScope(tenant, logger, "chat-export.fail", Reason);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await rls.ApplyAsync(cancellationToken);
        var job = await context.ChatExportJobs.FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);
        if (job is null || job.Status != ChatExportStatus.Running)
        {
            await transaction.RollbackAsync(cancellationToken);
            return;
        }

        job.Fail(code, clock.UtcNow);
        await Record(job, "failed", new ChatExportBuild(0, 0, 0, 0, code, 0), cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        context.ChangeTracker.Clear();
    }

    /// <summary>Deletes the files of exports past their 24 hours and queues again the jobs a stopped process left running.</summary>
    public async Task SweepAsync(CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        using var system = new SystemContextScope(tenant, logger, "chat-export.sweep", Reason);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await rls.ApplyAsync(cancellationToken);

        var expired = await context.ChatExportJobs
            .Where(j => j.Status == ChatExportStatus.Ready && j.ExpiresAt != null && j.ExpiresAt <= now)
            .OrderBy(j => j.ExpiresAt)
            .Take(100)
            .ToListAsync(cancellationToken);
        foreach (var job in expired)
        {
            if (job.StorageKey is not null)
                await storage.DeleteAsync(job.StorageKey, cancellationToken);
            job.Expire(now);
        }

        var staleBefore = now - StaleRunning;
        var stale = await context.ChatExportJobs
            .Where(j => j.Status == ChatExportStatus.Running && j.StartedAt != null && j.StartedAt < staleBefore)
            .Take(100)
            .ToListAsync(cancellationToken);
        foreach (var job in stale)
            job.Requeue(now);

        if (expired.Count + stale.Count == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return;
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}

/// <summary>Hosted loop of <see cref="ChatExportRunner"/>: takes queued exports one at a time, each in its own worker scope.</summary>
internal sealed class ChatExportService(IServiceScopeFactory scopeFactory, ILogger<ChatExportService> logger) : BackgroundService
{
    private static readonly TimeSpan Idle = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan SweepEvery = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var lastSweep = DateTimeOffset.MinValue;
        while (!stoppingToken.IsCancellationRequested)
        {
            var ran = false;
            try
            {
                await using var scope = scopeFactory.CreateWorkerAsyncScope();
                var runner = scope.ServiceProvider.GetRequiredService<ChatExportRunner>();
                ran = await runner.RunNextAsync(stoppingToken);
                if (!ran && DateTimeOffset.UtcNow - lastSweep > SweepEvery)
                {
                    await runner.SweepAsync(stoppingToken);
                    lastSweep = DateTimeOffset.UtcNow;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex) { logger.LogError(ex, "Chat export pass failed"); }

            if (ran) continue;
            try { await Task.Delay(Idle, stoppingToken); }
            catch (OperationCanceledException) { return; }
        }
    }
}
