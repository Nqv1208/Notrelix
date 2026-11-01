using System.Text.Json.Serialization;
using Notrelix.Application.Common.Storage;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Transfer;

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
            Kind = TransferKind.Import,
            Id = jobId,
            WorkspaceId = workspaceId,
            BoardId = request.BoardId,
            UserId = me.UserId,
            Status = TransferStatus.Uploaded,
            Format = data.Format,
            Total = data.TotalRows,
            OptionsJson = TransferJson.Write(options),
            CreatedAt = clock.UtcNow,
        });

        var columns = data.Headers.Select((h, i) => new ImportColumnDto(
            i, h, data.Rows.Select(r => r.Cells[i]).Where(c => !string.IsNullOrWhiteSpace(c)).Take(3).Select(c => c!.Length > 60 ? c[..60] : c).ToList())).ToList();
        return new ImportUploadDto(jobId, null, name, data.Format, bytes.Length, data.TotalRows, data.Headers.Count, columns, targets, ImportPlanner.Suggest(data.Headers, targets));
    }

    private static ImportUploadDto Refuse(string name, long size, string code, string format = "", int rows = 0, int columns = 0) =>
        new(null, code, name, format, size, rows, columns, [], [], []);
}
