using Notrelix.Application.Common.Storage;
using Notrelix.Application.Common.Tenancy;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Transfer;

public sealed record ExportOutcome(long Size, string? FailureCode);

/// <param name="Status">queued, running, ready, failed or expired.</param>
public sealed record ExportJobDto(
    Guid Id, string Status, string Format, string Scope, string FileName, int RowCount, int Processed, long? Size,
    DateTimeOffset? ExpiresAt, string? FailureCode, DateTimeOffset CreatedAt);

public sealed record ExportSummaryDto(int ViewCount, int AllCount, IReadOnlyList<string> HiddenFields);

public sealed record ExportContentDto(Stream Content, string FileName, string ContentType, long Size);

public sealed record ExportRequestBody(string Format, string Scope, string? Filter, string? Sort, string? Locale);

internal static class ExportJobs
{
    public static readonly TimeSpan FileLife = TimeSpan.FromHours(24);

    /// <summary>A guest may see a board but never takes its data out as a file.</summary>
    public static async Task RequireMemberAsync(IWorkspaceAccessResolver access, Guid workspaceId, Guid userId, CancellationToken ct)
    {
        if ((await access.ResolveAsync(workspaceId, userId, ct)).IsGuest)
            throw new ForbiddenException("Guests cannot export a board.", "work-management.export-guest");
    }

    public static async Task<TransferJob> OwnAsync(IBoardTransferJobs jobs, ICurrentRequestContext me, Guid boardId, Guid jobId, CancellationToken ct)
    {
        var job = await jobs.FindAsync(TransferKind.Export, jobId, ct);
        if (job is null || job.BoardId != boardId || job.UserId != me.UserId) throw new NotFoundException("BoardExport", jobId);
        return job;
    }

    public static ExportJobDto ToDto(TransferJob job, ITransferProgress progress, IDateTimeProvider clock)
    {
        var options = job.Options<ExportOptions>();
        var outcome = job.Detail<ExportOutcome>();
        var expired = job.Status == TransferStatus.Ready && job.ExpiresAt <= clock.UtcNow;
        var live = job.Status == TransferStatus.Running ? progress.Get(job.Id) : null;
        return new ExportJobDto(
            job.Id, expired ? TransferStatus.Expired : job.Status, job.Format, options.Scope, options.FileName, live?.Total ?? job.Total,
            live?.Processed ?? (job.Status == TransferStatus.Ready ? job.Total : 0), outcome?.Size, job.ExpiresAt, outcome?.FailureCode, job.CreatedAt);
    }
}
