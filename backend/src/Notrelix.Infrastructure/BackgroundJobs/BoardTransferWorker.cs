using Notrelix.Application.Common.SystemOperations;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Application.Features.WorkManagement.Common;
using Notrelix.Application.Features.WorkManagement.Ordering;
using Notrelix.Application.Features.WorkManagement.Transfer;
using Notrelix.Domain.Collaboration.Preferences;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Items;
using ExportOptions = Notrelix.Application.Features.WorkManagement.Transfer.ExportOptions;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Notifications;

namespace Notrelix.Infrastructure.BackgroundJobs;

/// <summary>
/// Runs the queued board file imports and exports (UC-WM-B29, B31) and removes what expired. Each run is one
/// transaction under the explicit system (worker) context, for the person who asked: an import creates all of its
/// items or none; an export re-checks that the person can still see the board before it reads a row.
/// </summary>
internal sealed class BoardTransferRunner(
    ApplicationDbContext context,
    ICurrentTenantContext tenant,
    IRlsSessionContext rls,
    IBoardTransferJobs jobs,
    IStorageService storage,
    ITransferProgress progress,
    IDateTimeProvider clock,
    IBoardItemKeyAllocator keys,
    IWorkOrderingScopeLock orderingLock,
    BoardExportBuilder exports,
    IResourceVisibility visibility,
    IWorkspaceAccessResolver access,
    IActorLookupService actors,
    ILogger<BoardTransferRunner> logger)
{
    private static readonly SystemOperationReason Reason = new("WorkManagement", "Run a queued board file import or export for the person who asked");
    public static readonly TimeSpan FileLife = TimeSpan.FromHours(24);

    /// <returns>True when a job was run, so the caller looks for the next one at once.</returns>
    public async Task<bool> RunNextAsync(CancellationToken ct)
    {
        TransferJob? job;
        await using (var tx = await context.Database.BeginTransactionAsync(ct))
        {
            job = await jobs.ClaimNextAsync(clock.UtcNow, ct);
            await tx.CommitAsync(ct);
        }

        if (job is null) return false;
        try
        {
            if (job.Kind == TransferKind.Import) await RunImportAsync(job, ct);
            else await RunExportAsync(job, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            logger.LogError(ex, "Board {Kind} {JobId} failed", job.Kind, job.Id);
            context.ChangeTracker.Clear();
            await FinishAsync(job, TransferStatus.Failed, ex.GetType().Name, ct, failureCode: "error");
        }
        finally
        {
            progress.Clear(job.Id);
        }

        return true;
    }

    // ----- import -----
    private async Task RunImportAsync(TransferJob job, CancellationToken ct)
    {
        var options = job.Options<ImportOptions>();
        var started = clock.UtcNow;
        ImportOutcome? outcome = null;
        using (new SystemContextScope(tenant, logger, "board-transfer.import", Reason))
        {
            await using var tx = await context.Database.BeginTransactionAsync(ct);
            await rls.ApplyAsync(ct);
            var board = await context.Boards.AsNoTracking().FirstOrDefaultAsync(b => b.Id == job.BoardId, ct);
            if (board is null || board.DeletedAt is not null)
                outcome = Failure("board-gone");
            else if (board.IsArchived)
                outcome = Failure("board-archived", board.UpdatedBy is { } who ? (await actors.FindAsync(who, ct))?.Name : null);
            else
            {
                var data = await ImportJobsRead(options, ct);
                var schema = new ImportSchema(
                    await context.BoardFields.AsNoTracking().Include(f => f.Options).Where(f => f.BoardId == job.BoardId && f.DeletedAt == null).ToListAsync(ct),
                    await context.BoardGroups.AsNoTracking().Where(g => g.BoardId == job.BoardId && g.DeletedAt == null).ToListAsync(ct));
                var evaluation = ImportPlanner.Evaluate(data, schema, options.Mapping ?? [], collectItems: true);
                if (evaluation.ErrorCount > 0)
                {
                    outcome = new ImportOutcome(0, evaluation.ErrorCount, evaluation.ErrorRows, evaluation.Errors,
                        evaluation.ErrorCount > evaluation.Errors.Count, null, null, "has-errors");
                }
                else
                {
                    outcome = await CreateItemsAsync(job, options, board, schema, evaluation.Items, ct);
                    job.Status = TransferStatus.Succeeded;
                    job.Succeeded = outcome.Created;
                }
            }

            if (outcome.FailureCode is not null)
            {
                await tx.RollbackAsync(ct);
            }
            else
            {
                job.CompletedAt = clock.UtcNow;
                job.StartedAt = started;
                job.DetailJson = TransferJson.Write(outcome);
                await jobs.UpdateAsync(job, ct);
                await context.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return;
            }
        }

        context.ChangeTracker.Clear();
        job.DetailJson = TransferJson.Write(outcome);
        job.Failed = outcome.ErrorCount;
        await FinishAsync(job, TransferStatus.Failed, outcome.FailureCode, ct, detailJson: job.DetailJson);

        static ImportOutcome Failure(string code, string? by = null) => new(0, 0, 0, [], false, null, null, code, by);
    }

    private async Task<TabularData> ImportJobsRead(ImportOptions options, CancellationToken ct)
    {
        await using var stream = await storage.OpenReadAsync(options.StorageKey, ct)
            ?? throw new InvalidOperationException("The import file is gone.");
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, ct);
        return TabularReader.Read(buffer.ToArray());
    }

    private async Task<ImportOutcome> CreateItemsAsync(
        TransferJob job, ImportOptions options, Board board, ImportSchema schema, IReadOnlyList<PlannedItem> items, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var status = WorkItemSemanticFields.ResolveStatusField(schema.Fields);
        var positions = new Dictionary<Guid, Queue<FractionalIndex>>();
        foreach (var group in items.GroupBy(i => i.GroupId).OrderBy(g => g.Key))
        {
            await orderingLock.AcquireAsync(new WorkOrderingScope(WorkOrderingScopeKind.GroupItems, group.Key), ct);
            var last = await context.BoardItems.AsNoTracking()
                .Where(i => i.GroupId == group.Key && i.ParentItemId == null && i.DeletedAt == null)
                .OrderByDescending(i => i.Position).Select(i => i.Position).FirstOrDefaultAsync(ct);
            positions[group.Key] = new Queue<FractionalIndex>(FractionalIndexGenerator.GenerateNKeysBetween(last, null, group.Count()));
        }

        var allocated = await keys.AllocateAsync(context, job.BoardId, items.Count, ct);
        for (var i = 0; i < items.Count; i++)
        {
            var planned = items[i];
            var item = BoardItem.CreateRoot(
                options.AccountId, job.WorkspaceId, job.BoardId, planned.GroupId, planned.Title, positions[planned.GroupId].Dequeue(),
                job.UserId, now, itemKey: allocated[i].Key, itemSequence: allocated[i].Sequence);
            context.BoardItems.Add(item);
            foreach (var (field, value) in planned.Values)
                BoardItemFieldWrites.Write(item, field, value, status, job.UserId, now);

            if ((i + 1) % 250 == 0 || i == items.Count - 1)
            {
                await context.SaveChangesAsync(ct);
                context.ChangeTracker.Clear();
                progress.Set(job.Id, "creating", i + 1, items.Count);
            }
        }

        return new ImportOutcome(items.Count, 0, 0, [], false, allocated.Count > 0 ? allocated[0].Key : null, allocated.Count > 0 ? allocated[^1].Key : null, null);
    }

    // ----- export -----
    private async Task RunExportAsync(TransferJob job, CancellationToken ct)
    {
        var options = job.Options<ExportOptions>();
        string? failure = null;
        string? key = null;
        long size = 0;
        int rowCount = 0;
        using (new SystemContextScope(tenant, logger, "board-transfer.export", Reason))
        {
            await using var tx = await context.Database.BeginTransactionAsync(ct);
            await rls.ApplyAsync(ct);
            var board = await context.Boards.AsNoTracking().FirstOrDefaultAsync(b => b.Id == job.BoardId, ct);
            var visible = board is null || board.DeletedAt is not null ? [] :
                (await visibility.VisibleBoardIdsAsync(job.WorkspaceId, job.UserId, ct))
                    .Concat(await visibility.VisibleArchivedBoardIdsAsync(job.WorkspaceId, job.UserId, ct)).ToList();
            if (!visible.Contains(job.BoardId) || (await access.ResolveAsync(job.WorkspaceId, job.UserId, ct)).IsGuest)
            {
                failure = "no-access";
            }
            else
            {
                var table = await exports.BuildAsync(job.BoardId, options.Scope, options.Filter, options.Sort, options.Locale,
                    (done, total) => progress.Set(job.Id, "writing", done, total), ct);
                rowCount = table.Rows.Count;
                using var file = new MemoryStream();
                TabularWriter.Write(job.Format, table.Headers, table.Rows, file);
                size = file.Length;
                file.Position = 0;
                key = $"{options.AccountId:N}/{job.WorkspaceId:N}/transfers/{job.Id:N}";
                await storage.WriteAsync(key, file, job.Format == TabularWriter.Xlsx ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" : "text/csv", ct);
            }

            if (failure is null)
            {
                try
                {
                    var now = clock.UtcNow;
                    job.Status = TransferStatus.Ready;
                    job.Total = rowCount;
                    job.StorageKey = key;
                    job.CompletedAt = now;
                    job.ExpiresAt = now + FileLife;
                    job.DetailJson = TransferJson.Write(new ExportOutcome(size, null));
                    await jobs.UpdateAsync(job, ct);
                    Notify(job, options, now);
                    await context.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                    return;
                }
                catch
                {
                    await storage.DeleteAsync(key!, CancellationToken.None);
                    throw;
                }
            }

            await tx.RollbackAsync(ct);
        }

        context.ChangeTracker.Clear();
        await FinishAsync(job, TransferStatus.Failed, failure, ct, failureCode: failure);
    }

    private void Notify(TransferJob job, ExportOptions options, DateTimeOffset now)
    {
        var vi = options.Locale != "en";
        var title = vi ? $"Tệp xuất “{options.FileName}” đã sẵn sàng" : $"Your export “{options.FileName}” is ready";
        var body = vi
            ? $"Tệp xuất “{options.FileName}” đã sẵn sàng. Tải ở Xuất việc của board, trong 24 giờ."
            : $"Your export “{options.FileName}” is ready. Download it from the board's Export, within 24 hours.";
        var item = NotificationItemRecord.Create(
            accountId: options.AccountId, workspaceId: job.WorkspaceId, sourceContext: "work-management",
            notificationType: NotificationTypes.ExportReadyProducedType, severity: NotificationSeverity.Info, title: title, createdAt: now,
            body: body, deduplicationKey: $"{NotificationTypes.ExportReadyProducedType}:{job.Id:N}:{job.UserId}", expiresAt: now + FileLife);
        context.NotificationItems.Add(item);
        context.NotificationRecipients.Add(NotificationRecipientRecord.Create(
            accountId: options.AccountId, notificationId: item.Id, workspaceId: job.WorkspaceId, recipientUserId: job.UserId, createdAt: now));
    }

    private async Task FinishAsync(TransferJob job, string status, string? message, CancellationToken ct, string? failureCode = null, string? detailJson = null)
    {
        using var _ = new SystemContextScope(tenant, logger, "board-transfer.finish", Reason);
        await using var tx = await context.Database.BeginTransactionAsync(ct);
        await rls.ApplyAsync(ct);
        job.Status = status;
        job.CompletedAt = clock.UtcNow;
        job.ErrorMessage = message;
        if (detailJson is not null) job.DetailJson = detailJson;
        else if (job.Kind == TransferKind.Export) job.DetailJson = TransferJson.Write(new ExportOutcome(0, failureCode));
        else job.DetailJson = TransferJson.Write(new ImportOutcome(0, 0, 0, [], false, null, null, failureCode));
        await jobs.UpdateAsync(job, ct);
        await context.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    // ----- expiry -----
    /// <summary>Deletes export files past their 24 hours, and the source files of old imports; closes imports a stopped process left running.</summary>
    public async Task SweepAsync(CancellationToken ct)
    {
        var now = clock.UtcNow;
        IReadOnlyList<TransferJob> expired, stale;
        await using (var tx = await context.Database.BeginTransactionAsync(ct))
        {
            expired = await jobs.ListExpiredExportsAsync(now, ct);
            stale = await jobs.ListStaleImportsAsync(now, ct);
            await tx.CommitAsync(ct);
        }

        if (expired.Count + stale.Count == 0) return;
        using var system = new SystemContextScope(tenant, logger, "board-transfer.sweep", Reason);
        await using var sweep = await context.Database.BeginTransactionAsync(ct);
        await rls.ApplyAsync(ct);
        foreach (var job in expired)
        {
            if (job.StorageKey is not null) await storage.DeleteAsync(job.StorageKey, ct);
            job.Status = TransferStatus.Expired;
            job.StorageKey = null;
            await jobs.UpdateAsync(job, ct);
        }

        foreach (var job in stale)
        {
            await storage.DeleteAsync(job.Options<ImportOptions>().StorageKey, ct);
            if (job.Status == TransferStatus.Running)
            {
                job.Status = TransferStatus.Failed;
                job.DetailJson = TransferJson.Write(new ImportOutcome(0, 0, 0, [], false, null, null, "error"));
            }

            job.ExpiresAt = now;
            await jobs.UpdateAsync(job, ct);
        }

        await context.SaveChangesAsync(ct);
        await sweep.CommitAsync(ct);
    }
}

/// <summary>Hosted loop of <see cref="BoardTransferRunner"/>: takes queued jobs one at a time, each in its own worker scope.</summary>
internal sealed class BoardTransferService(IServiceScopeFactory scopeFactory, ILogger<BoardTransferService> logger) : BackgroundService
{
    private static readonly TimeSpan Idle = TimeSpan.FromSeconds(2);
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
                var runner = scope.ServiceProvider.GetRequiredService<BoardTransferRunner>();
                ran = await runner.RunNextAsync(stoppingToken);
                if (!ran && DateTimeOffset.UtcNow - lastSweep > SweepEvery)
                {
                    await runner.SweepAsync(stoppingToken);
                    lastSweep = DateTimeOffset.UtcNow;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex) { logger.LogError(ex, "Board transfer pass failed"); }

            if (ran) continue;
            try { await Task.Delay(Idle, stoppingToken); }
            catch (OperationCanceledException) { return; }
        }
    }
}
