using System.Text.Json.Serialization;
using FluentValidation.Results;
using Notrelix.Application.Common.Data;
using Notrelix.Application.Common.Storage;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Transfer;

public sealed record ImportColumnDto(int Index, string Header, IReadOnlyList<string> Examples);

/// <param name="JobId">Null when the file was refused; <paramref name="Rejection"/> then says why (unsupported-type, no-rows, too-many-rows, too-many-columns, unsafe-content, malformed).</param>
public sealed record ImportUploadDto(
    Guid? JobId, string? Rejection, string FileName, string Format, long Size, int RowCount, int ColumnCount,
    IReadOnlyList<ImportColumnDto> Columns, IReadOnlyList<ImportTarget> Targets, IReadOnlyList<ColumnMap> Suggested);

public sealed record ImportPreviewDto(
    int RowCount, bool MissingTitle, IReadOnlyList<ColumnCheck> Checks, int ErrorCount, int ErrorRows,
    IReadOnlyList<ImportRowError> Errors, IReadOnlyList<SampleRow> Sample);

/// <param name="Status">uploaded, queued, running, succeeded or failed.</param>
/// <param name="Phase">While running: reading (checking the file) or creating.</param>
public sealed record ImportJobDto(
    Guid Id, string Status, string? Phase, int Total, int Processed, int Created, int ErrorCount, int ErrorRows,
    string? FirstKey, string? LastKey, string? FailureCode, string FileName, DateTimeOffset CreatedAt, string? FailureBy = null);

public sealed record ImportErrorsDto(IReadOnlyList<ImportRowError> Errors, bool Truncated, IReadOnlyList<string> Headers, string FileName);

public sealed record ImportMappingBody(IReadOnlyList<ColumnMap> Mapping);

internal static class ImportJobs
{
    public static readonly ResourceKind BoardKind = ResourceKind.Create("work-management.board");

    /// <summary>The caller's own job on this board; anything else is reported as not found.</summary>
    public static async Task<TransferJob> OwnAsync(IBoardTransferJobs jobs, ICurrentRequestContext me, Guid boardId, Guid jobId, CancellationToken ct)
    {
        var job = await jobs.FindAsync(TransferKind.Import, jobId, ct);
        if (job is null || job.BoardId != boardId || job.UserId != me.UserId) throw new NotFoundException("BoardImport", jobId);
        return job;
    }

    public static async Task<ImportSchema> SchemaAsync(IWorkManagementDbContext context, Guid boardId, CancellationToken ct)
    {
        var fields = await context.BoardFields.AsNoTracking().Include(f => f.Options)
            .Where(f => f.BoardId == boardId && f.DeletedAt == null).ToListAsync(ct);
        var groups = await context.BoardGroups.AsNoTracking().Where(g => g.BoardId == boardId && g.DeletedAt == null).ToListAsync(ct);
        return new ImportSchema(fields, groups);
    }

    public static async Task<TabularData> ReadAsync(IStorageService storage, ImportOptions options, CancellationToken ct)
    {
        await using var stream = await storage.OpenReadAsync(options.StorageKey, ct) ?? throw new NotFoundException("BoardImportFile", options.StorageKey);
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, ct);
        return TabularReader.Read(buffer.ToArray());
    }

    public static ImportJobDto ToDto(TransferJob job, ITransferProgress progress)
    {
        var options = job.Options<ImportOptions>();
        var outcome = job.Detail<ImportOutcome>();
        var live = job.Status == TransferStatus.Running ? progress.Get(job.Id) : null;
        return new ImportJobDto(
            job.Id, job.Status, live?.Phase, job.Total, live?.Processed ?? (job.Status == TransferStatus.Succeeded ? job.Total : 0),
            outcome?.Created ?? 0, outcome?.ErrorCount ?? 0, outcome?.ErrorRows ?? 0, outcome?.FirstKey, outcome?.LastKey,
            outcome?.FailureCode, options.FileName, job.CreatedAt, outcome?.FailureBy);
    }

    public static void RequireTitle(IReadOnlyList<ColumnMap> mapping, ImportSchema schema)
    {
        var title = schema.TitleField?.Id.ToString();
        if (!mapping.Any(m => m.Target == ImportPlanner.Title || (title is not null && m.Target == title)))
            throw new Notrelix.Application.Common.Exceptions.ValidationException([new ValidationFailure("mapping", "missing-title")]);
    }
}
