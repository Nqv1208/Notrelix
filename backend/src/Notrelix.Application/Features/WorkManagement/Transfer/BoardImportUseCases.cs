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

/// <summary>
/// Step 1 of importing items from a file (UC-WM-B29/B30): reads the file on the server (CSV or XLSX, checked by its
/// bytes, at most 5,000 rows), keeps it with the caller's import and says what columns it has and where they would
/// land. Nothing is created. A file that cannot be used is answered with a reason, not an error status.
/// </summary>
public record StartBoardImportCommand(Guid BoardId, string FileName, [property: JsonIgnore] Stream Content)
    : ICommand<ImportUploadDto>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.CreateItem;
    public ResourceRef Resource => ResourceRef.Create(ImportJobs.BoardKind, BoardId);
}

public class StartBoardImportCommandHandler(
    IWorkManagementDbContext context,
    IBoardTransferJobs jobs,
    IStorageService storage,
    IRequestRollbackCompensations compensations,
    ICurrentRequestContext me,
    IDateTimeProvider clock) : IRequestHandler<StartBoardImportCommand, ImportUploadDto>
{
    public async Task<ImportUploadDto> Handle(StartBoardImportCommand request, CancellationToken ct)
    {
        var name = Path.GetFileName(request.FileName.Replace('\\', '/'));
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await request.Content.ReadAsync(chunk, ct)) > 0)
        {
            if (buffer.Length + read > TabularReader.MaxFileBytes) return Refuse(name, buffer.Length, "too-large");
            buffer.Write(chunk, 0, read);
        }

        var bytes = buffer.ToArray();
        TabularData data;
        try { data = TabularReader.Read(bytes); }
        catch (TabularFileException ex) { return Refuse(name, bytes.Length, ex.Code); }
        if (data.TotalRows > TabularReader.MaxRows)
            return Refuse(name, bytes.Length, "too-many-rows", data.Format, data.TotalRows, data.Headers.Count);

        var schema = await ImportJobs.SchemaAsync(context, request.BoardId, ct);
        var targets = ImportPlanner.Targets(schema);
        var accountId = me.RequireAccountId();
        var workspaceId = me.RequireWorkspaceId();
        var jobId = Guid.CreateVersion7();
        var key = $"{accountId:N}/{workspaceId:N}/transfers/{jobId:N}";
        compensations.Register($"delete import file {key}", token => storage.DeleteAsync(key, token));
        await storage.WriteAsync(key, new MemoryStream(bytes), data.Format == "xlsx" ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" : "text/csv", ct);

        var options = new ImportOptions(accountId, name, data.Format, bytes.Length, key, data.TotalRows, data.Headers, null);
        jobs.Add(new TransferJob
        {
            Kind = TransferKind.Import, Id = jobId, WorkspaceId = workspaceId, BoardId = request.BoardId, UserId = me.UserId,
            Status = TransferStatus.Uploaded, Format = data.Format, Total = data.TotalRows, OptionsJson = TransferJson.Write(options),
            CreatedAt = clock.UtcNow,
        });

        var columns = data.Headers.Select((h, i) => new ImportColumnDto(
            i, h, data.Rows.Select(r => r.Cells[i]).Where(c => !string.IsNullOrWhiteSpace(c)).Take(3).Select(c => c!.Length > 60 ? c[..60] : c).ToList())).ToList();
        return new ImportUploadDto(jobId, null, name, data.Format, bytes.Length, data.TotalRows, data.Headers.Count, columns, targets, ImportPlanner.Suggest(data.Headers, targets));
    }

    private static ImportUploadDto Refuse(string name, long size, string code, string format = "", int rows = 0, int columns = 0) =>
        new(null, code, name, format, size, rows, columns, [], [], []);
}

/// <summary>Step 2 and 3: checks a mapping against the stored file and the board as it is now. Reads only.</summary>
/// <param name="AllErrors">Return every stored error (for the report) instead of the first 20.</param>
public record PreviewBoardImportQuery(Guid BoardId, Guid JobId, IReadOnlyList<ColumnMap> Mapping, bool AllErrors = false)
    : IQuery<ImportPreviewDto>, IReadRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.CreateItem;
    public ResourceRef Resource => ResourceRef.Create(ImportJobs.BoardKind, BoardId);
}

public class PreviewBoardImportQueryHandler(
    IWorkManagementDbContext context, IBoardTransferJobs jobs, IStorageService storage, ICurrentRequestContext me)
    : IRequestHandler<PreviewBoardImportQuery, ImportPreviewDto>
{
    public async Task<ImportPreviewDto> Handle(PreviewBoardImportQuery request, CancellationToken ct)
    {
        var job = await ImportJobs.OwnAsync(jobs, me, request.BoardId, request.JobId, ct);
        if (job.Status != TransferStatus.Uploaded) throw new ConflictException("The import was already sent.");
        var data = await ImportJobs.ReadAsync(storage, job.Options<ImportOptions>(), ct);
        var schema = await ImportJobs.SchemaAsync(context, request.BoardId, ct);
        var missing = !request.Mapping.Any(m => m.Target == ImportPlanner.Title || (schema.TitleField is { } t && m.Target == t.Id.ToString()));
        var result = ImportPlanner.Evaluate(data, schema, request.Mapping, collectItems: false);
        return new ImportPreviewDto(
            data.TotalRows, missing, result.Checks, result.ErrorCount, result.ErrorRows, result.Errors.Take(request.AllErrors ? ImportPlanner.MaxStoredErrors : 20).ToList(), result.Sample);
    }
}

/// <summary>
/// Step 4: sends the import. The file must check clean under this mapping, all or nothing; the creating itself runs in
/// the background so closing the dialog is safe. Repeating the call (same key or not) never queues a second run.
/// </summary>
[IdempotencyOperation("work-management.board-imports.confirm.v1")]
public record ConfirmBoardImportCommand(Guid BoardId, Guid JobId, IReadOnlyList<ColumnMap> Mapping)
    : ICommand<ImportJobDto>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.CreateItem;
    public ResourceRef Resource => ResourceRef.Create(ImportJobs.BoardKind, BoardId);
}

public class ConfirmBoardImportCommandHandler(
    IWorkManagementDbContext context, IBoardTransferJobs jobs, IStorageService storage, ITransferProgress progress, ICurrentRequestContext me)
    : IRequestHandler<ConfirmBoardImportCommand, ImportJobDto>
{
    public async Task<ImportJobDto> Handle(ConfirmBoardImportCommand request, CancellationToken ct)
    {
        var job = await ImportJobs.OwnAsync(jobs, me, request.BoardId, request.JobId, ct);
        if (job.Status != TransferStatus.Uploaded) return ImportJobs.ToDto(job, progress);

        var options = job.Options<ImportOptions>();
        var schema = await ImportJobs.SchemaAsync(context, request.BoardId, ct);
        ImportJobs.RequireTitle(request.Mapping, schema);
        var evaluation = ImportPlanner.Evaluate(await ImportJobs.ReadAsync(storage, options, ct), schema, request.Mapping, collectItems: false);
        if (evaluation.ErrorCount > 0)
            throw new Notrelix.Application.Common.Exceptions.ValidationException([new ValidationFailure("file", "has-errors")]);

        job.OptionsJson = TransferJson.Write(options with { Mapping = request.Mapping });
        job.Status = TransferStatus.Queued;
        await jobs.UpdateAsync(job, ct);
        return ImportJobs.ToDto(job, progress);
    }
}

public record GetBoardImportQuery(Guid BoardId, Guid JobId)
    : IQuery<ImportJobDto>, IReadRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.CreateItem;
    public ResourceRef Resource => ResourceRef.Create(ImportJobs.BoardKind, BoardId);
}

public class GetBoardImportQueryHandler(IBoardTransferJobs jobs, ITransferProgress progress, ICurrentRequestContext me)
    : IRequestHandler<GetBoardImportQuery, ImportJobDto>
{
    public async Task<ImportJobDto> Handle(GetBoardImportQuery request, CancellationToken ct) =>
        ImportJobs.ToDto(await ImportJobs.OwnAsync(jobs, me, request.BoardId, request.JobId, ct), progress);
}

/// <summary>The rows a refused import could not take, for the downloadable report (the client words the reasons).</summary>
public record GetBoardImportErrorsQuery(Guid BoardId, Guid JobId)
    : IQuery<ImportErrorsDto>, IReadRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.CreateItem;
    public ResourceRef Resource => ResourceRef.Create(ImportJobs.BoardKind, BoardId);
}

public class GetBoardImportErrorsQueryHandler(IBoardTransferJobs jobs, ICurrentRequestContext me)
    : IRequestHandler<GetBoardImportErrorsQuery, ImportErrorsDto>
{
    public async Task<ImportErrorsDto> Handle(GetBoardImportErrorsQuery request, CancellationToken ct)
    {
        var job = await ImportJobs.OwnAsync(jobs, me, request.BoardId, request.JobId, ct);
        var options = job.Options<ImportOptions>();
        var outcome = job.Detail<ImportOutcome>();
        return new ImportErrorsDto(outcome?.Errors ?? [], outcome?.ErrorsTruncated ?? false, options.Headers, options.FileName);
    }
}
