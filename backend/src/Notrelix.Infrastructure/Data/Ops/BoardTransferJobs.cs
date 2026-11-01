using System.Collections.Concurrent;
using System.Text.Json;
using Notrelix.Application.Features.WorkManagement.Transfer;
using Notrelix.Infrastructure.Data.Ops.Entities;

namespace Notrelix.Infrastructure.Data.Ops;

/// <summary>
/// The import and export jobs of the Ops schema, read and written for the Work Management transfer use cases. Rows
/// are scoped to a workspace by row-level security; the worker's own login reaches every workspace.
/// </summary>
internal sealed class BoardTransferJobs(ApplicationDbContext context) : IBoardTransferJobs
{
    private const string JobType = "board-items";
    private const string BoardKind = "work-management.board";

    public void Add(TransferJob job)
    {
        if (job.Kind == TransferKind.Import)
        {
            var row = ImportJobRecord.Create(job.Id, job.WorkspaceId, JobType, job.Status, job.OptionsJson, job.CreatedAt);
            row.TargetResourceType = BoardKind;
            row.TargetResourceId = job.BoardId;
            row.RequestedByUserId = job.UserId;
            row.TotalRecords = job.Total;
            context.ImportJobs.Add(row);
        }
        else
        {
            var row = ExportJobRecord.Create(job.Id, job.WorkspaceId, JobType, job.Status, job.Format, job.OptionsJson, job.DetailJson ?? "{}", job.CreatedAt);
            row.SourceResourceType = BoardKind;
            row.SourceResourceId = job.BoardId;
            row.RequestedByUserId = job.UserId;
            context.ExportJobs.Add(row);
        }
    }

    public async Task<TransferJob?> FindAsync(TransferKind kind, Guid id, CancellationToken ct)
    {
        if (kind == TransferKind.Import)
        {
            var row = await context.ImportJobs.FirstOrDefaultAsync(j => j.Id == id, ct);
            return row is null ? null : From(row);
        }

        var export = await context.ExportJobs.FirstOrDefaultAsync(j => j.Id == id, ct);
        return export is null ? null : From(export);
    }

    public async Task UpdateAsync(TransferJob job, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        if (job.Kind == TransferKind.Import)
        {
            var row = await context.ImportJobs.FirstAsync(j => j.Id == job.Id, ct);
            row.Status = job.Status;
            row.TotalRecords = job.Total;
            row.SucceededRecords = job.Succeeded;
            row.FailedRecords = job.Failed;
            row.OptionsJson = job.OptionsJson;
            row.ResultJson = job.DetailJson;
            row.ErrorMessage = job.ErrorMessage;
            row.StartedAt = job.StartedAt;
            row.CompletedAt = job.CompletedAt;
            row.ExpiresAt = job.ExpiresAt;
            row.UpdatedAt = now;
        }
        else
        {
            var row = await context.ExportJobs.FirstAsync(j => j.Id == job.Id, ct);
            row.Status = job.Status;
            row.RowCount = job.Total;
            row.OptionsJson = job.OptionsJson;
            row.FiltersJson = job.DetailJson ?? "{}";
            row.StorageKey = job.StorageKey;
            row.StorageProvider = job.StorageKey is null ? null : "local";
            row.ExpiresAt = job.ExpiresAt;
            row.ErrorMessage = job.ErrorMessage;
            row.StartedAt = job.StartedAt;
            row.CompletedAt = job.CompletedAt;
            row.UpdatedAt = now;
        }

        await Task.CompletedTask;
    }

    public async Task<IReadOnlyList<TransferJob>> ListExportsAsync(Guid boardId, Guid userId, DateTimeOffset since, CancellationToken ct) =>
        (await context.ExportJobs.AsNoTracking()
            .Where(j => j.SourceResourceId == boardId && j.RequestedByUserId == userId && j.CreatedAt >= since)
            .OrderByDescending(j => j.CreatedAt).Take(5).ToListAsync(ct)).Select(From).ToList();

    public async Task<TransferJob?> ClaimNextAsync(DateTimeOffset now, CancellationToken ct)
    {
        var import = await context.Database.SqlQuery<Guid>($"""
            UPDATE ops.import_jobs SET status = 'running', started_at = {now}, updated_at = {now}
            WHERE id = (SELECT id FROM ops.import_jobs WHERE status = 'queued' ORDER BY created_at LIMIT 1 FOR UPDATE SKIP LOCKED)
            RETURNING id AS "Value"
            """).ToListAsync(ct);
        if (import.Count == 1)
        {
            var row = await context.ImportJobs.AsNoTracking().FirstAsync(j => j.Id == import[0], ct);
            return From(row);
        }

        var export = await context.Database.SqlQuery<Guid>($"""
            UPDATE ops.export_jobs SET status = 'running', started_at = {now}, updated_at = {now}
            WHERE id = (SELECT id FROM ops.export_jobs WHERE status = 'queued' ORDER BY created_at LIMIT 1 FOR UPDATE SKIP LOCKED)
            RETURNING id AS "Value"
            """).ToListAsync(ct);
        if (export.Count == 1)
        {
            var row = await context.ExportJobs.AsNoTracking().FirstAsync(j => j.Id == export[0], ct);
            return From(row);
        }

        return null;
    }

    public async Task<IReadOnlyList<TransferJob>> ListExpiredExportsAsync(DateTimeOffset now, CancellationToken ct) =>
        (await context.ExportJobs.AsNoTracking().Where(j => j.Status == "ready" && j.ExpiresAt <= now).Take(50).ToListAsync(ct)).Select(From).ToList();

    public async Task<IReadOnlyList<TransferJob>> ListStaleImportsAsync(DateTimeOffset now, CancellationToken ct)
    {
        var day = now.AddHours(-24);
        var hour = now.AddMinutes(-30);
        return (await context.ImportJobs.AsNoTracking()
            .Where(j => (j.ExpiresAt == null && j.CreatedAt < day && j.Status != "queued" && j.Status != "running")
                        || (j.Status == "running" && j.StartedAt < hour))
            .Take(50).ToListAsync(ct)).Select(From).ToList();
    }

    private static TransferJob From(ImportJobRecord r)
    {
        using var options = JsonDocument.Parse(r.OptionsJson);
        return new TransferJob
        {
            Kind = TransferKind.Import,
            Id = r.Id,
            WorkspaceId = r.WorkspaceId,
            BoardId = r.TargetResourceId ?? Guid.Empty,
            UserId = r.RequestedByUserId ?? Guid.Empty,
            Status = r.Status,
            Total = r.TotalRecords,
            Succeeded = r.SucceededRecords,
            Failed = r.FailedRecords,
            Format = options.RootElement.TryGetProperty("format", out var f) ? f.GetString() ?? "" : "",
            OptionsJson = r.OptionsJson,
            DetailJson = r.ResultJson,
            ErrorMessage = r.ErrorMessage,
            ExpiresAt = r.ExpiresAt,
            CreatedAt = r.CreatedAt,
            StartedAt = r.StartedAt,
            CompletedAt = r.CompletedAt,
        };
    }

    private static TransferJob From(ExportJobRecord r) => new()
    {
        Kind = TransferKind.Export,
        Id = r.Id,
        WorkspaceId = r.WorkspaceId,
        BoardId = r.SourceResourceId ?? Guid.Empty,
        UserId = r.RequestedByUserId ?? Guid.Empty,
        Status = r.Status,
        Format = r.Format,
        Total = r.RowCount ?? 0,
        OptionsJson = r.OptionsJson,
        DetailJson = r.FiltersJson,
        StorageKey = r.StorageKey,
        ExpiresAt = r.ExpiresAt,
        ErrorMessage = r.ErrorMessage,
        CreatedAt = r.CreatedAt,
        StartedAt = r.StartedAt,
        CompletedAt = r.CompletedAt,
    };
}

internal sealed class TransferProgress : ITransferProgress
{
    private readonly ConcurrentDictionary<Guid, (string Phase, int Processed, int Total)> _live = new();

    public void Set(Guid jobId, string phase, int processed, int total) => _live[jobId] = (phase, processed, total);

    public (string Phase, int Processed, int Total)? Get(Guid jobId) => _live.TryGetValue(jobId, out var v) ? v : null;

    public void Clear(Guid jobId) => _live.TryRemove(jobId, out _);
}
